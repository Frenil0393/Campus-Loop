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

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        CampusLoopDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
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

        // Enforce @ddu.ac.in college email requirement
        if (!model.Email.Trim().EndsWith("@ddu.ac.in", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("Email", "Registration is restricted to DDU students. Please use your official @ddu.ac.in email address.");
            return View(model);
        }

        // College email check
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
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
            await _userManager.AddToRoleAsync(user, Roles.Student);
            await _signInManager.SignInAsync(user, isPersistent: false);
            TempData["SuccessMessage"] = "Account created successfully! Welcome to CampusLoop.";
            return RedirectToAction("Index", "Home");
        }

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

        // Validate @ddu.ac.in domain for students (admin accounts exempt)
        if (!model.Email.Trim().EndsWith("@ddu.ac.in", StringComparison.OrdinalIgnoreCase) &&
            !model.Email.Trim().StartsWith("admin@", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Only official @ddu.ac.in student email addresses can sign in.");
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or account is inactive.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
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

        ModelState.AddModelError(string.Empty, "Invalid login credentials.");
        return View(model);
    }

    // POST: /Account/Logout (R.1.3 & R.8.2)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
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
                foreach (var err in passResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                return View(model);
            }
        }

        await _userManager.UpdateAsync(user);
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

        TempData["SuccessMessage"] = "Your account has been deleted.";
        return RedirectToAction("Login", "Account");
    }

    // Access Denied
    public IActionResult AccessDenied()
    {
        return View();
    }
}
