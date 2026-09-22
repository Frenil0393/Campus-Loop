using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusLoop.Data;
using CampusLoop.Models;

namespace CampusLoop.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly CampusLoopDbContext _context;

    public AdminController(CampusLoopDbContext context)
    {
        _context = context;
    }

    // ==========================================
    // R.12 ADMIN DASHBOARD AND STATISTICS
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var totalStudents = await _context.Users.CountAsync(u => u.Role == "Student");
        var totalProducts = await _context.Products.CountAsync();
        var availableProducts = await _context.Products.CountAsync(p => p.Status == "AVAILABLE");
        var soldProducts = await _context.Products.CountAsync(p => p.Status == "SOLD");
        var totalCategories = await _context.Categories.CountAsync(c => c.Slug != "all");
        var totalOrders = await _context.PaymentOrders.CountAsync();

        ViewBag.TotalStudents = totalStudents;
        ViewBag.TotalProducts = totalProducts;
        ViewBag.AvailableProducts = availableProducts;
        ViewBag.SoldProducts = soldProducts;
        ViewBag.TotalCategories = totalCategories;
        ViewBag.TotalOrders = totalOrders;

        // Recent listings and students
        ViewBag.RecentProducts = await _context.Products
            .Include(p => p.Seller)
            .Include(p => p.Category)
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .ToListAsync();

        ViewBag.RecentStudents = await _context.Users
            .Where(u => u.Role == "Student")
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .ToListAsync();

        return View();
    }

    // ==========================================
    // R.9 STUDENT MANAGEMENT
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Students()
    {
        var students = await _context.Users
            .Include(u => u.Products)
            .Where(u => u.Role == "Student")
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        return View(students);
    }

    [HttpGet]
    public async Task<IActionResult> StudentDetails(string id)
    {
        var student = await _context.Users
            .Include(u => u.Products)
                .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (student == null) return NotFound("Student not found.");

        return View(student);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveStudent(string id)
    {
        var student = await _context.Users
            .Include(u => u.Products)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (student != null)
        {
            _context.Products.RemoveRange(student.Products);
            _context.Users.Remove(student);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Student '{student.FullName}' has been removed successfully.";
        }

        return RedirectToAction(nameof(Students));
    }

    // ==========================================
    // R.10 PRODUCT MANAGEMENT - ADMIN
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Products()
    {
        var products = await _context.Products
            .Include(p => p.Seller)
            .Include(p => p.Category)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return View(products);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Product '{product.Name}' was removed by Administrator.";
        }

        return RedirectToAction(nameof(Products));
    }

    // ==========================================
    // R.11 CATEGORY MANAGEMENT - ADMIN
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Categories()
    {
        var categories = await _context.Categories
            .Include(c => c.Products)
            .Where(c => c.Slug != "all")
            .ToListAsync();

        return View(categories);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(string name, string slug, string iconClass, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Category name is required.";
            return RedirectToAction(nameof(Categories));
        }

        slug = string.IsNullOrWhiteSpace(slug) ? name.ToLower().Replace(" ", "-") : slug.ToLower();

        if (await _context.Categories.AnyAsync(c => c.Name.ToLower() == name.ToLower() || c.Slug == slug))
        {
            TempData["Error"] = "A category with this name or slug already exists.";
            return RedirectToAction(nameof(Categories));
        }

        var category = new Category
        {
            Name = name.Trim(),
            Slug = slug.Trim(),
            IconClass = string.IsNullOrWhiteSpace(iconClass) ? "bi-box-seam" : iconClass.Trim(),
            Description = description?.Trim() ?? ""
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Category '{category.Name}' added successfully!";
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(int id, string name, string slug, string iconClass, string description)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();

        category.Name = name.Trim();
        category.Slug = slug.Trim().ToLower();
        category.IconClass = iconClass.Trim();
        category.Description = description?.Trim() ?? "";

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Category '{category.Name}' updated successfully!";
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null) return NotFound();

        // R.11.3: Check whether products are associated with the category
        if (category.Products.Any())
        {
            TempData["Error"] = $"Cannot delete category '{category.Name}' because it has {category.Products.Count} active products assigned.";
            return RedirectToAction(nameof(Categories));
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Category '{category.Name}' deleted successfully.";
        return RedirectToAction(nameof(Categories));
    }
}
