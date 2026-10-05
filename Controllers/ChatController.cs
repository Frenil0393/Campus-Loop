using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using CampusLoop.Data;
using CampusLoop.Hubs;
using CampusLoop.Models;
using CampusLoop.ViewModels;

namespace CampusLoop.Controllers;

[Authorize]
public class ChatController : Controller
{
    private readonly CampusLoopDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        CampusLoopDbContext context,
        UserManager<ApplicationUser> userManager,
        IHubContext<ChatHub> hubContext,
        ILogger<ChatController> logger)
    {
        _context = context;
        _userManager = userManager;
        _hubContext = hubContext;
        _logger = logger;
    }

    // GET: /Chat (Inbox)
    public async Task<IActionResult> Index()
    {
        var currentUserId = _userManager.GetUserId(User)!;
        var conversations = await GetUserConversations(currentUserId);

        if (conversations.Any())
        {
            return RedirectToAction(nameof(Conversation), new { id = conversations.First().ConversationId });
        }

        return View(new ChatRoomViewModel
        {
            CurrentUserId = currentUserId,
            AllConversations = conversations
        });
    }

    // GET: /Chat/StartChat?productId=5 (R.6.1, R.7.2)
    public async Task<IActionResult> StartChat(int productId)
    {
        var currentUserId = _userManager.GetUserId(User)!;
        var product = await _context.Products.FindAsync(productId);

        if (product == null) return NotFound();

        // Prevent seller chatting with themselves
        if (product.SellerId == currentUserId)
        {
            _logger.LogWarning("Self-chat attempt blocked: User {UserId} tried to chat on their own listing {ProductId}", currentUserId, productId);
            TempData["ErrorMessage"] = "You cannot initiate a chat on your own listing.";
            return RedirectToAction("Details", "Products", new { id = productId });
        }

        // Find existing conversation for this product and buyer
        var conversation = await _context.ChatConversations
            .FirstOrDefaultAsync(c => c.ProductId == productId && c.BuyerId == currentUserId);

        if (conversation == null)
        {
            conversation = new ChatConversation
            {
                ProductId = productId,
                BuyerId = currentUserId,
                SellerId = product.SellerId,
                CreatedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow
            };
            _context.ChatConversations.Add(conversation);
            await _context.SaveChangesAsync();
            _logger.LogInformation("New chat conversation created: {ConversationId} between Buyer {BuyerId} and Seller {SellerId} for Product {ProductId}",
                conversation.Id, currentUserId, product.SellerId, productId);
        }

        return RedirectToAction(nameof(Conversation), new { id = conversation.Id });
    }

    // GET: /Chat/Conversation/5 (R.6.4, R.6.5)
    public async Task<IActionResult> Conversation(int id)
    {
        var currentUserId = _userManager.GetUserId(User)!;

        var conv = await _context.ChatConversations.FindAsync(id);
        if (conv == null) return NotFound();

        // Ensure user is buyer or seller
        if (conv.BuyerId != currentUserId && conv.SellerId != currentUserId)
        {
            return Forbid();
        }

        var product = await _context.Products.FindAsync(conv.ProductId);
        var otherUserId = (conv.BuyerId == currentUserId) ? conv.SellerId : conv.BuyerId;
        var otherUser = await _userManager.FindByIdAsync(otherUserId);

        var primaryImage = await _context.ProductImages
            .Where(img => img.ProductId == conv.ProductId)
            .OrderByDescending(img => img.IsPrimary)
            .Select(img => img.ImageUrl)
            .FirstOrDefaultAsync() ?? "/images/placeholder.png";

        // Mark messages as read
        var unreadMessages = await _context.ChatMessages
            .Where(m => m.ConversationId == id && m.SenderId != currentUserId && !m.IsRead)
            .ToListAsync();

        if (unreadMessages.Any())
        {
            foreach (var msg in unreadMessages) msg.IsRead = true;
        }

        // Mark conversation notifications as read
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == currentUserId && n.ConversationId == id && !n.IsRead)
            .ToListAsync();

        if (unreadNotifications.Any())
        {
            foreach (var n in unreadNotifications) n.IsRead = true;
        }

        if (unreadMessages.Any() || unreadNotifications.Any())
        {
            await _context.SaveChangesAsync();
        }

        var messages = await _context.ChatMessages
            .Where(m => m.ConversationId == id)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        var messageVms = messages.Select(m => new ChatMessageItemViewModel
        {
            Id = m.Id,
            SenderId = m.SenderId,
            SenderName = (m.SenderId == currentUserId) ? "You" : (otherUser?.FullName ?? "Student"),
            MessageText = m.MessageText,
            SentAt = m.SentAt,
            IsFromCurrentUser = (m.SenderId == currentUserId)
        }).ToList();

        var allConversations = await GetUserConversations(currentUserId);

        var vm = new ChatRoomViewModel
        {
            ConversationId = conv.Id,
            ProductId = conv.ProductId,
            ProductTitle = product?.Title ?? "Product",
            ProductPrice = product?.Price ?? 0,
            ProductImageUrl = primaryImage,
            ProductStatus = product?.Status ?? ProductStatus.Available,
            OtherUserId = otherUserId,
            OtherUserName = otherUser?.FullName ?? "Student",
            OtherUserBranch = otherUser?.Branch ?? "",
            OtherUserSemester = otherUser?.Semester ?? 1,
            OtherUserPhone = otherUser?.PhoneNumber ?? "",
            CurrentUserId = currentUserId,
            Messages = messageVms,
            AllConversations = allConversations
        };

        return View(vm);
    }

    // POST: /Chat/SendMessage (R.6.2, R.6.3)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(int conversationId, string messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText))
        {
            return BadRequest("Message cannot be empty.");
        }

        var currentUserId = _userManager.GetUserId(User)!;
        var conv = await _context.ChatConversations.FindAsync(conversationId);
        if (conv == null) return NotFound();

        if (conv.BuyerId != currentUserId && conv.SellerId != currentUserId)
        {
            return Forbid();
        }

        var message = new ChatMessage
        {
            ConversationId = conversationId,
            SenderId = currentUserId,
            MessageText = messageText.Trim(),
            SentAt = DateTime.UtcNow,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        conv.LastMessageAt = DateTime.UtcNow;

        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();

        var sender = await _userManager.FindByIdAsync(currentUserId);
        var otherUserId = (conv.BuyerId == currentUserId) ? conv.SellerId : conv.BuyerId;
        var product = await _context.Products.FindAsync(conv.ProductId);
        var senderName = sender?.FullName ?? "Student";
        var productTitle = product?.Title ?? "item";
        var notificationMessage = $"New message from {senderName} about {productTitle}";
        var conversationUrl = Url.Action(nameof(Conversation), "Chat", new { id = conversationId }) ?? $"/Chat/Conversation/{conversationId}";

        var existingNotification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.UserId == otherUserId && n.ConversationId == conversationId && !n.IsRead);

        if (existingNotification != null)
        {
            existingNotification.Message = notificationMessage;
            existingNotification.LinkUrl = conversationUrl;
            existingNotification.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            var notification = new Notification
            {
                UserId = otherUserId,
                Message = notificationMessage,
                LinkUrl = conversationUrl,
                IsRead = false,
                ConversationId = conversationId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
        }
        await _context.SaveChangesAsync();

        var timeFormatted = message.SentAt.ToLocalTime().ToString("hh:mm tt");

        // Broadcast to SignalR group (R.6.3)
        await _hubContext.Clients.Group($"conv_{conversationId}").SendAsync("ReceiveMessage", new
        {
            id = message.Id,
            conversationId = conversationId,
            senderId = currentUserId,
            senderName = sender?.FullName ?? "Student",
            messageText = message.MessageText,
            sentAt = timeFormatted,
            isCurrentUser = false
        });

        _logger.LogInformation("Chat message {MessageId} sent in Conversation {ConversationId} by User {SenderId}",
            message.Id, conversationId, currentUserId);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new 
            { 
                success = true, 
                messageId = message.Id,
                senderId = currentUserId,
                senderName = sender?.FullName ?? "You",
                messageText = message.MessageText,
                sentAt = timeFormatted
            });
        }

        return RedirectToAction(nameof(Conversation), new { id = conversationId });
    }

    private async Task<List<ChatConversationItemViewModel>> GetUserConversations(string userId)
    {
        var convs = await _context.ChatConversations
            .Where(c => c.BuyerId == userId || c.SellerId == userId)
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync();

        var productIds = convs.Select(c => c.ProductId).Distinct().ToList();
        var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        var images = await _context.ProductImages
            .Where(img => productIds.Contains(img.ProductId) && img.IsPrimary)
            .ToDictionaryAsync(img => img.ProductId, img => img.ImageUrl);

        var otherUserIds = convs.Select(c => (c.BuyerId == userId) ? c.SellerId : c.BuyerId).Distinct().ToList();
        var users = await _context.Users.Where(u => otherUserIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName);

        var convIds = convs.Select(c => c.Id).ToList();
        var lastMessages = await _context.ChatMessages
            .Where(m => convIds.Contains(m.ConversationId))
            .GroupBy(m => m.ConversationId)
            .Select(g => g.OrderByDescending(m => m.SentAt).FirstOrDefault())
            .ToListAsync();

        var result = new List<ChatConversationItemViewModel>();

        foreach (var c in convs)
        {
            products.TryGetValue(c.ProductId, out var prod);
            var otherId = (c.BuyerId == userId) ? c.SellerId : c.BuyerId;
            users.TryGetValue(otherId, out var otherName);
            images.TryGetValue(c.ProductId, out var imgUrl);
            var lastMsg = lastMessages.FirstOrDefault(m => m?.ConversationId == c.Id);

            result.Add(new ChatConversationItemViewModel
            {
                ConversationId = c.Id,
                ProductId = c.ProductId,
                ProductTitle = prod?.Title ?? "Product",
                ProductPrice = prod?.Price ?? 0,
                ProductStatus = prod?.Status ?? ProductStatus.Available,
                ProductImageUrl = imgUrl ?? "/images/placeholder.png",
                OtherUserId = otherId,
                OtherUserName = otherName ?? "Student",
                LastMessage = lastMsg?.MessageText ?? "No messages yet",
                LastMessageAt = c.LastMessageAt,
                HasUnread = lastMsg != null && lastMsg.SenderId != userId && !lastMsg.IsRead
            });
        }

        return result;
    }
}
