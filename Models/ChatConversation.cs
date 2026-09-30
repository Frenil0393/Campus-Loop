using System.ComponentModel.DataAnnotations;

namespace CampusLoop.Models;

public class ChatConversation : BaseEntity
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    public string BuyerId { get; set; } = string.Empty;

    [Required]
    public string SellerId { get; set; } = string.Empty;

    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
}
