using System.ComponentModel.DataAnnotations;

namespace CampusLoop.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "College email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid college email")]
    [Display(Name = "College Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }
}
