using System.ComponentModel.DataAnnotations;

namespace CampusLoop.Models;

public class Category
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(60)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(60)]
    public string IconClass { get; set; } = "bi-box-seam";

    public string Description { get; set; } = string.Empty;

    public int ItemCount { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
