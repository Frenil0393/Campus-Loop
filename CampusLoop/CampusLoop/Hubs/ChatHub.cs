using Microsoft.AspNetCore.SignalR;
using CampusLoop.Data;
using CampusLoop.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusLoop.Hubs;

public class ChatHub : Hub
{
    private readonly CampusLoopDbContext _context;

    public ChatHub(CampusLoopDbContext context)
    {
        _context = context;
    }

    // Join a product-specific chat channel between buyer and seller
    public async Task JoinChatRoom(int productId, string otherUserId)
    {
        var currentUserId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId)) return;

        // Generate consistent room name regardless of who opens it
        var roomName = GetRoomName(productId, currentUserId, otherUserId);
        await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
    }

    // Send real-time message (R.6.2 & R.6.3)
    public async Task SendMessage(int productId, string receiverId, string messageText)
    {
        var senderId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(senderId) || string.IsNullOrWhiteSpace(messageText)) return;

        var sender = await _context.Users.FindAsync(senderId);
        var receiver = await _context.Users.FindAsync(receiverId);
        var product = await _context.Products.FindAsync(productId);

        if (sender == null || receiver == null || product == null) return;

        // Store message in database (R.6.2 & R.6.3)
        var chatMsg = new DbChatMessage
        {
            ProductId = productId,
            SenderId = senderId,
            ReceiverId = receiverId,
            MessageText = messageText.Trim(),
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.ChatMessages.Add(chatMsg);
        await _context.SaveChangesAsync();

        var roomName = GetRoomName(productId, senderId, receiverId);

        // Broadcast to both users in the room via SignalR in real-time without page refresh (R.6.3)
        await Clients.Group(roomName).SendAsync("ReceiveMessage", new
        {
            id = chatMsg.Id,
            productId = productId,
            senderId = senderId,
            senderName = sender.FullName,
            senderAvatar = sender.AvatarUrl,
            message = chatMsg.MessageText,
            sentAt = chatMsg.SentAt.ToString("hh:mm tt"),
            isOwn = false // Client handles check against own user ID
        });
    }

    public static string GetRoomName(int productId, string userA, string userB)
    {
        var sortedUsers = new[] { userA, userB }.OrderBy(u => u).ToArray();
        return $"chat_prod_{productId}_{sortedUsers[0]}_{sortedUsers[1]}";
    }
}
