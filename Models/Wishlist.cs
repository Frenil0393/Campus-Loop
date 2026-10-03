using System.ComponentModel.DataAnnotations;

namespace CampusLoop.Models;

public class Wishlist : BaseEntity
{
    [Required]
    public string StudentId { get; set; } = string.Empty;

    [Required]
    public int ProductId { get; set; }

    public ApplicationUser? Student { get; set; }

    public Product? Product { get; set; }
}
