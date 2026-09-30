using System.ComponentModel.DataAnnotations;

namespace CampusLoop.Models;

public class ChatMessage : BaseEntity
{
    [Required]
    public int ConversationId { get; set; }

    [Required]
    public string SenderId { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string MessageText { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; } = false;
}
