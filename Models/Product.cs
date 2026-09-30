using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusLoop.Models;

public class Product : BaseEntity
{
    [Required, MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required, Range(0.01, 1000000.00)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required]
    public string SellerId { get; set; } = string.Empty;

    public ProductStatus Status { get; set; } = ProductStatus.Available;

    public DateTime? UpdatedAt { get; set; }
}
