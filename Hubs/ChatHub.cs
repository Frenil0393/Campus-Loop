using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CampusLoop.Hubs;

public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinConversation(string conversationId)
    {
        _logger.LogInformation("SignalR connection {ConnectionId} joined conversation group {ConversationId}",
            Context.ConnectionId, conversationId);
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conv_{conversationId}");
    }

    public async Task LeaveConversation(string conversationId)
    {
        _logger.LogInformation("SignalR connection {ConnectionId} left conversation group {ConversationId}",
            Context.ConnectionId, conversationId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conv_{conversationId}");
    }
}
