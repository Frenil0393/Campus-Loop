using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusLoop.Data;
using CampusLoop.Models;

namespace CampusLoop.Controllers;

[Authorize]
public class ChatController : Controller
{
    private readonly CampusLoopDbContext _context;

    public ChatController(CampusLoopDbContext context)
    {
        _context = context;
    }

    // Inbox listing all chats of the logged-in student
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Get all unique product chats where user is sender or receiver
        var messages = await _context.ChatMessages
            .Include(m => m.Product)
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
            .OrderByDescending(m => m.SentAt)
            .ToListAsync();

        // Group by product and conversation partner
        var conversations = messages
            .GroupBy(m => new
            {
                m.ProductId,
                PartnerId = m.SenderId == currentUserId ? m.ReceiverId : m.SenderId
            })
            .Select(g =>
            {
                var latest = g.First();
                var partner = latest.SenderId == currentUserId ? latest.Receiver : latest.Sender;
                return new
                {
                    ProductId = g.Key.ProductId,
                    Product = latest.Product,
                    Partner = partner,
                    LatestMessage = latest.MessageText,
                    LatestTime = latest.SentAt,
                    UnreadCount = g.Count(m => m.ReceiverId == currentUserId && !m.IsRead)
                };
            })
            .ToList();

        ViewBag.Conversations = conversations;
        return View();
    }

    // R.6.1 & R.6.4 & R.6.5: Open specific conversation for a product
    [HttpGet]
    public async Task<IActionResult> Conversation(int productId, string? sellerId = null)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Seller)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null) return NotFound("Product not found");

        var partnerId = sellerId ?? product.SellerId;
        if (partnerId == currentUserId)
        {
            // If seller opens chat, partner is the last buyer who messaged
            var lastMsg = await _context.ChatMessages
                .Where(m => m.ProductId == productId && m.ReceiverId == currentUserId)
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefaultAsync();

            partnerId = lastMsg?.SenderId ?? product.SellerId;
        }

        var partner = await _context.Users.FindAsync(partnerId);

        // Retrieve conversation history (R.6.4)
        var chatHistory = await _context.ChatMessages
            .Include(m => m.Sender)
            .Where(m => m.ProductId == productId &&
                       ((m.SenderId == currentUserId && m.ReceiverId == partnerId) ||
                        (m.SenderId == partnerId && m.ReceiverId == currentUserId)))
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        // Mark unread messages as read
        var unread = chatHistory.Where(m => m.ReceiverId == currentUserId && !m.IsRead).ToList();
        foreach (var m in unread)
        {
            m.IsRead = true;
        }
        if (unread.Any())
        {
            await _context.SaveChangesAsync();
        }

        ViewBag.Product = product;
        ViewBag.Partner = partner;
        ViewBag.CurrentUserId = currentUserId;

        return View(chatHistory);
    }

    // R.6.2: Send Message via C# Backend Form
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(int productId, string receiverId, string messageText)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return RedirectToAction("Login", "Account");
        }

        if (!string.IsNullOrWhiteSpace(messageText))
        {
            var chatMsg = new DbChatMessage
            {
                ProductId = productId,
                SenderId = currentUserId,
                ReceiverId = receiverId,
                MessageText = messageText.Trim(),
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.ChatMessages.Add(chatMsg);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Conversation), new { productId, sellerId = receiverId });
    }
}
