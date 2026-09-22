using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusLoop.Data;
using CampusLoop.Models;

namespace CampusLoop.Controllers;

public class AccountController : Controller
{
    private readonly CampusLoopDbContext _context;

    public AccountController(CampusLoopDbContext context)
    {
        _context = context;
    }

    // ==========================================
    // R.1.1 STUDENT REGISTRATION
    // ==========================================
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        string fullName, 
        string collegeEmail, 
        string phoneNumber, 
        string branch, 
        string semester, 
        string password,
        string confirmPassword)
    {
        // 1. Validate inputs
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(collegeEmail) ||
            string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(branch) ||
            string.IsNullOrWhiteSpace(semester) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "All fields are required. Please fill in complete details.";
            return View();
        }

        if (password != confirmPassword)
        {
            ViewBag.Error = "Passwords do not match.";
            return View();
        }

        if (password.Length < 6)
        {
            ViewBag.Error = "Password must be at least 6 characters long.";
            return View();
        }

        // 2. Validate college email
        collegeEmail = collegeEmail.Trim().ToLower();
        if (!collegeEmail.Contains("@") || !collegeEmail.Contains("."))
        {
            ViewBag.Error = "Please provide a valid college email address.";
            return View();
        }

        // 3. Check whether email already exists
        if (await _context.Users.AnyAsync(u => u.CollegeEmail.ToLower() == collegeEmail))
        {
            ViewBag.Error = "An account with this college email already exists. Please login instead.";
            return View();
        }

        // 4. Create new student user with Student role
        var student = new StudentUser
        {
            Id = "std_" + Guid.NewGuid().ToString("N").Substring(0, 8),
            FullName = fullName.Trim(),
            CollegeEmail = collegeEmail,
            PhoneNumber = phoneNumber.Trim(),
            Branch = branch.Trim(),
            Semester = semester.Trim(),
            PasswordHash = password, // In production, use PBKDF2/BCrypt
            Role = "Student",
            IsActive = true,
            AvatarUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(student);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Registration successful! You can now log in to CampusLoop.";
        return RedirectToAction(nameof(Login));
    }

    // ==========================================
    // R.1.2 & R.8.1 STUDENT & ADMIN LOGIN
    // ==========================================
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Please enter both college email and password.";
            return View();
        }

        email = email.Trim().ToLower();

        // 1. Authenticate user credentials
        var user = await _context.Users.FirstOrDefaultAsync(u => u.CollegeEmail.ToLower() == email && u.PasswordHash == password);
        if (user == null)
        {
            ViewBag.Error = "Invalid email or password. Please try again.";
            return View();
        }

        // 2. Verify account is active
        if (!user.IsActive)
        {
            ViewBag.Error = "Your student account has been deactivated by the campus administrator.";
            return View();
        }

        // 3. Create authenticated cookie session with Claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.CollegeEmail),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("Branch", user.Branch),
            new Claim("Semester", user.Semester),
            new Claim("AvatarUrl", user.AvatarUrl ?? "")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTime.UtcNow.AddDays(7)
        });

        // 4. Redirect based on role (R.8: Admin goes to Dashboard, Student goes to Home)
        if (user.Role == "Admin")
        {
            return RedirectToAction("Dashboard", "Admin");
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    // ==========================================
    // R.1.3 & R.8.2 LOGOUT
    // ==========================================
    [HttpPost]
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete("CampusLoop.Auth");
        TempData["Success"] = "You have been logged out successfully.";
        return RedirectToAction("Login", "Account");
    }

    // ==========================================
    // R.1.4 VIEW PROFILE
    // ==========================================
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var student = await _context.Users
            .Include(u => u.Products)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (student == null)
        {
            return NotFound("Student profile not found.");
        }

        return View(student);
    }

    // ==========================================
    // R.1.5 UPDATE PROFILE
    // ==========================================
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(string fullName, string phoneNumber, string branch, string semester, string? newPassword)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var student = await _context.Users.FindAsync(userId);
        if (student == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(fullName)) student.FullName = fullName.Trim();
        if (!string.IsNullOrWhiteSpace(phoneNumber)) student.PhoneNumber = phoneNumber.Trim();
        if (!string.IsNullOrWhiteSpace(branch)) student.Branch = branch.Trim();
        if (!string.IsNullOrWhiteSpace(semester)) student.Semester = semester.Trim();

        // Optional password update
        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            if (newPassword.Length < 6)
            {
                TempData["Error"] = "New password must be at least 6 characters long.";
                return RedirectToAction(nameof(Profile));
            }
            student.PasswordHash = newPassword;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Profile updated successfully!";
        return RedirectToAction(nameof(Profile));
    }

    // ==========================================
    // R.1.6 DELETE ACCOUNT
    // ==========================================
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var student = await _context.Users
            .Include(u => u.Products)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (student != null)
        {
            // Remove associated products and account
            _context.Products.RemoveRange(student.Products);
            _context.Users.Remove(student);
            await _context.SaveChangesAsync();
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Success"] = "Your student account and all listings have been deleted.";
        return RedirectToAction(nameof(Login));
    }

    // Access Denied page
    public IActionResult AccessDenied()
    {
        return View();
    }
}
