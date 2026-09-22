using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusLoop.Models;

public class Product
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Required]
    public int CategoryId { get; set; }
    [ForeignKey("CategoryId")]
    public virtual Category? Category { get; set; }

    [Required]
    public string SellerId { get; set; } = string.Empty;
    [ForeignKey("SellerId")]
    public virtual StudentUser? Seller { get; set; }
    
    // 3-4 product images per R.3.1 and R.14.1
    public List<string> ImageUrls { get; set; } = new();
    
    // Status: "AVAILABLE" or "SOLD" per R.3.5 and R.3.6
    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "AVAILABLE";
    
    [MaxLength(40)]
    public string Condition { get; set; } = "Good"; // e.g. "Like New", "Gently Used", "Fair"

    [MaxLength(120)]
    public string CampusLocation { get; set; } = "Main Campus";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public bool IsWishlisted { get; set; } = false;
}
