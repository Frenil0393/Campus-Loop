using System.ComponentModel.DataAnnotations;

namespace CampusLoop.Models;

public class StudentUser
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(120)]
    public string CollegeEmail { get; set; } = string.Empty;

    [Required]
    [Phone]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string Branch { get; set; } = string.Empty;

    [Required]
    [MaxLength(40)]
    public string Semester { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string AvatarUrl { get; set; } = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150";

    public string Role { get; set; } = "Student"; // "Student" or "Admin"

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
