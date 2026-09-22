using System.ComponentModel.DataAnnotations;

namespace CampusLoop.Models;

public class ProductImage : BaseEntity
{
    [Required]
    public int ProductId { get; set; }

    [Required, MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    public bool IsPrimary { get; set; } = false;
}
