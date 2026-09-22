using System.ComponentModel.DataAnnotations;

namespace CampusLoop.ViewModels;

public class ProfileViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public int Semester { get; set; }
    public DateTime MemberSince { get; set; }
    public int TotalListed { get; set; }
    public int TotalSold { get; set; }
}

public class EditProfileViewModel
{
    [Required(ErrorMessage = "Full Name is required")]
    [MaxLength(100)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required")]
    [Phone]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Branch is required")]
    [MaxLength(100)]
    [Display(Name = "Branch")]
    public string Branch { get; set; } = string.Empty;

    [Required(ErrorMessage = "Semester is required")]
    [Range(1, 8)]
    [Display(Name = "Semester")]
    public int Semester { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "New Password (leave blank to keep current)")]
    public string? NewPassword { get; set; }
}
