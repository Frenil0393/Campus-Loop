using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusLoop.Models;

public class ProductImage
{
    [Key]
    public int Id { get; set; }

    public int ProductId { get; set; }
    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }

    [Required]
    public string ImageUrl { get; set; } = string.Empty;

    public bool IsPrimary { get; set; } = false;
}

public class DbChatMessage
{
    [Key]
    public int Id { get; set; }

    public int ProductId { get; set; }
    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }

    [Required]
    public string SenderId { get; set; } = string.Empty;
    [ForeignKey("SenderId")]
    public virtual StudentUser? Sender { get; set; }

    [Required]
    public string ReceiverId { get; set; } = string.Empty;
    [ForeignKey("ReceiverId")]
    public virtual StudentUser? Receiver { get; set; }

    [Required]
    public string MessageText { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; } = false;
}

public class WishlistItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    [ForeignKey("UserId")]
    public virtual StudentUser? User { get; set; }

    public int ProductId { get; set; }
    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
