using System.ComponentModel.DataAnnotations;

namespace CampusLoop.Models;

public abstract class BaseEntity
{
    [Key]
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
