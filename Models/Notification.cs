using System.ComponentModel.DataAnnotations;

namespace CampusLoop.Models;

public class Notification : BaseEntity
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string LinkUrl { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;

    public int? ConversationId { get; set; }

    public ChatConversation? Conversation { get; set; }
}
