using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CampusLoop.Data;
using CampusLoop.Models;
using CampusLoop.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CampusLoop.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly CampusLoopDbContext _context;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        CampusLoopDbContext context,
        RoleManager<IdentityRole> roleManager,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _roleManager = roleManager;
        _logger = logger;
    }

    // GET: /Account/Register (R.1.1)
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    // POST: /Account/Register (R.1.1)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Enforce official DDU college email requirement (@ddu.ac.in only)
        var email = model.Email.Trim();
        if (!email.EndsWith("@ddu.ac.in", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Registration attempt rejected: {Email} does not use official @ddu.ac.in domain", model.Email);
            ModelState.AddModelError("Email", "Registration is restricted to DDU students. You must use an official @ddu.ac.in email address.");
            return View(model);
        }

        // College email check
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Registration failed: User with email {Email} already exists", model.Email);
            ModelState.AddModelError("Email", "An account with this college email already exists.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            PhoneNumber = model.PhoneNumber,
            Branch = model.Branch,
            Semester = model.Semester,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            // Assign Student role by default
            // Make sure the Student role exists
            if (!await _roleManager.RoleExistsAsync(Roles.Student))
            {
                await _roleManager.CreateAsync(new IdentityRole(Roles.Student));
            }

            var roleResult = await _userManager.AddToRoleAsync(user, Roles.Student);
            if (!roleResult.Succeeded)
            {
                // Don't leave a user without a role
                await _userManager.DeleteAsync(user);
                _logger.LogError("Failed to assign Student role to {Email}: {Errors}", model.Email,
                    string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            _logger.LogInformation("Student registered successfully: {Email} (Name: {Name}, Branch: {Branch}, Sem: {Semester})",
                user.Email, user.FullName, user.Branch, user.Semester);

            await _signInManager.SignInAsync(user, isPersistent: false);
            TempData["SuccessMessage"] = "Account created successfully! Welcome to CampusLoop.";
            return RedirectToAction("Index", "Home");
        }

        _logger.LogWarning("User creation failed for {Email}: {Errors}", model.Email,
            string.Join(", ", result.Errors.Select(e => e.Description)));

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    // GET: /Account/Login (R.1.2 & R.8.1)
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST: /Account/Login (R.1.2 & R.8.1)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Validate official DDU college email requirement (@ddu.ac.in only)
        var loginEmail = model.Email.Trim();
        if (!loginEmail.EndsWith("@ddu.ac.in", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Login rejected for non-DDU email: {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "Please enter your official @ddu.ac.in college email address.");
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("Login failed for {Email}: User not found or inactive", model.Email);
            ModelState.AddModelError(string.Empty, "Invalid email or account is inactive.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            _logger.LogInformation("User logged in successfully: {Email}", user.Email);

            if (await _userManager.IsInRoleAsync(user, Roles.Admin))
            {
                return RedirectToAction("Index", "Admin");
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        _logger.LogWarning("Invalid password attempt for user {Email}", model.Email);
        ModelState.AddModelError(string.Empty, "Invalid login credentials.");
        return View(model);
    }

    // POST: /Account/Logout (R.1.3 & R.8.2)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userName = User.Identity?.Name ?? "Unknown";
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out: {UserName}", userName);
        return RedirectToAction("Login", "Account");
    }

    // GET: /Account/Profile (R.1.4)
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var totalListed = await _context.Products.CountAsync(p => p.SellerId == user.Id);
        var totalSold = await _context.Products.CountAsync(p => p.SellerId == user.Id && p.Status == ProductStatus.Sold);

        var vm = new ProfileViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? "",
            PhoneNumber = user.PhoneNumber ?? "",
            Branch = user.Branch,
            Semester = user.Semester,
            MemberSince = user.CreatedAt,
            TotalListed = totalListed,
            TotalSold = totalSold
        };

        return View(vm);
    }

    // GET: /Account/EditProfile (R.1.5)
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> EditProfile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var vm = new EditProfileViewModel
        {
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber ?? "",
            Branch = user.Branch,
            Semester = user.Semester
        };

        return View(vm);
    }

    // POST: /Account/EditProfile (R.1.5)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> EditProfile(EditProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        user.FullName = model.FullName;
        user.PhoneNumber = model.PhoneNumber;
        user.Branch = model.Branch;
        user.Semester = model.Semester;

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
            if (!passResult.Succeeded)
            {
                _logger.LogWarning("Password update failed for user {UserId}: {Errors}", user.Id,
                    string.Join(", ", passResult.Errors.Select(e => e.Description)));
                foreach (var err in passResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                return View(model);
            }
            _logger.LogInformation("Password updated for user {UserId}", user.Id);
        }

        await _userManager.UpdateAsync(user);
        _logger.LogInformation("Profile updated for user {UserId} ({Email})", user.Id, user.Email);
        TempData["SuccessMessage"] = "Profile updated successfully!";
        return RedirectToAction(nameof(Profile));
    }

    // POST: /Account/DeleteAccount (R.1.6)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> DeleteAccount()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        // Sign out first
        await _signInManager.SignOutAsync();

        // Soft delete / deactivate or remove
        user.IsActive = false;
        await _userManager.UpdateAsync(user);
        _logger.LogWarning("Account deactivated by user: {UserId} ({Email})", user.Id, user.Email);

        TempData["SuccessMessage"] = "Your account has been deleted.";
        return RedirectToAction("Login", "Account");
    }

    // Access Denied
    public IActionResult AccessDenied()
    {
        var userId = _userManager.GetUserId(User) ?? "Anonymous";
        _logger.LogWarning("Access Denied for user {UserId} accessing {Path}", userId, Request.Path);
        return View();
    }
}
