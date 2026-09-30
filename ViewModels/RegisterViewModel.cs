using System.ComponentModel.DataAnnotations;

namespace CampusLoop.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Full Name is required")]
    [MaxLength(100)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "College email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@ddu\.ac\.in$", ErrorMessage = "Registration is restricted to DDU students. You must use an official @ddu.ac.in email address.")]
    [Display(Name = "DDU College Email (@ddu.ac.in)")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Please enter a valid phone number")]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Branch is required")]
    [MaxLength(100)]
    [Display(Name = "Branch / Department")]
    public string Branch { get; set; } = string.Empty; // e.g. Computer Engineering

    [Required(ErrorMessage = "Semester is required")]
    [Range(1, 8, ErrorMessage = "Semester must be between 1 and 8")]
    [Display(Name = "Current Semester")]
    public int Semester { get; set; } = 1;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
