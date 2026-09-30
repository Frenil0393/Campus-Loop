using System.ComponentModel.DataAnnotations;

namespace CampusLoop.Models;

public class Category : BaseEntity
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Description { get; set; }
}
