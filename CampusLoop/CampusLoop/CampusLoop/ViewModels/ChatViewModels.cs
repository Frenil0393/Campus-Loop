using CampusLoop.Models;

namespace CampusLoop.ViewModels;

public class ChatConversationItemViewModel
{
    public int ConversationId { get; set; }
    public int ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public decimal ProductPrice { get; set; }
    public string ProductImageUrl { get; set; } = string.Empty;
    public ProductStatus ProductStatus { get; set; }
    public string OtherUserId { get; set; } = string.Empty;
    public string OtherUserName { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
    public bool HasUnread { get; set; }
}

public class ChatMessageItemViewModel
{
    public int Id { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsFromCurrentUser { get; set; }
}

public class ChatRoomViewModel
{
    public int ConversationId { get; set; }
    public int ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public decimal ProductPrice { get; set; }
    public string ProductImageUrl { get; set; } = string.Empty;
    public ProductStatus ProductStatus { get; set; }

    public string OtherUserId { get; set; } = string.Empty;
    public string OtherUserName { get; set; } = string.Empty;
    public string OtherUserBranch { get; set; } = string.Empty;
    public int OtherUserSemester { get; set; }
    public string OtherUserPhone { get; set; } = string.Empty;

    public string CurrentUserId { get; set; } = string.Empty;

    public List<ChatMessageItemViewModel> Messages { get; set; } = new();
    public List<ChatConversationItemViewModel> AllConversations { get; set; } = new();
}
