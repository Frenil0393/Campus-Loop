using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusLoop.Data;
using CampusLoop.Models;
using CampusLoop.ViewModels;

namespace CampusLoop.Controllers;

[Authorize(Roles = Roles.Admin)]
public class AdminController : Controller
{
    private readonly CampusLoopDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        CampusLoopDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<AdminController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    // GET: /Admin (Dashboard - R.12.1)
    public async Task<IActionResult> Index()
    {
        _logger.LogInformation("Admin dashboard accessed by {AdminUser}", User.Identity?.Name ?? "Admin");

        var totalStudents = await _userManager.GetUsersInRoleAsync(Roles.Student);
        var totalProducts = await _context.Products.CountAsync();
        var availableProducts = await _context.Products.CountAsync(p => p.Status == ProductStatus.Available);
        var soldProducts = await _context.Products.CountAsync(p => p.Status == ProductStatus.Sold);
        var totalCategories = await _context.Categories.CountAsync();

        var vm = new AdminDashboardViewModel
        {
            TotalStudents = totalStudents.Count,
            TotalProducts = totalProducts,
            AvailableProducts = availableProducts,
            SoldProducts = soldProducts,
            TotalCategories = totalCategories
        };

        // Also pass recent products
        ViewBag.RecentProducts = await _context.Products
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .ToListAsync();

        return View(vm);
    }

    // GET: /Admin/Students (R.9.1)
    public async Task<IActionResult> Students()
    {
        var students = await _userManager.GetUsersInRoleAsync(Roles.Student);
        return View(students.OrderByDescending(s => s.CreatedAt).ToList());
    }

    // GET: /Admin/StudentDetails/id (R.9.2)
    public async Task<IActionResult> StudentDetails(string id)
    {
        var student = await _userManager.FindByIdAsync(id);
        if (student == null) return NotFound();

        var listings = await _context.Products
            .Where(p => p.SellerId == id)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        ViewBag.Listings = listings;
        return View(student);
    }

    // POST: /Admin/ToggleStudentStatus/id (R.9.3)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStudentStatus(string id)
    {
        var student = await _userManager.FindByIdAsync(id);
        if (student == null) return NotFound();

        student.IsActive = !student.IsActive;
        await _userManager.UpdateAsync(student);

        _logger.LogInformation("Admin {AdminUser} toggled student status for {StudentId} ({Email}). Active: {IsActive}",
            User.Identity?.Name, student.Id, student.Email, student.IsActive);

        TempData["SuccessMessage"] = student.IsActive ? "Student account reactivated." : "Student account deactivated.";
        return RedirectToAction(nameof(Students));
    }

    // GET: /Admin/Products (R.10.1)
    public async Task<IActionResult> Products()
    {
        var products = await _context.Products
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var categories = await _context.Categories.ToDictionaryAsync(c => c.Id, c => c.Name);
        var sellers = await _context.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);

        ViewBag.Categories = categories;
        ViewBag.Sellers = sellers;

        return View(products);
    }

    // POST: /Admin/RemoveProduct/id (R.10.3)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        // If the product has an order, do not delete it
        var hasOrder = await _context.Orders.AnyAsync(o => o.ProductId == id);
        if (hasOrder)
        {
            _logger.LogWarning("Admin {AdminUser} attempted to delete product {ProductId} ({Title}) which has associated orders",
                User.Identity?.Name, id, product.Title);
            TempData["ErrorMessage"] = "This item has a purchase record and cannot be deleted.";
            return RedirectToAction(nameof(Products));
        }

        // Delete associated records
        var images = await _context.ProductImages.Where(i => i.ProductId == id).ToListAsync();
        _context.ProductImages.RemoveRange(images);

        var wishlists = await _context.Wishlists.Where(w => w.ProductId == id).ToListAsync();
        _context.Wishlists.RemoveRange(wishlists);

        var convs = await _context.ChatConversations.Where(c => c.ProductId == id).ToListAsync();
        var convIds = convs.Select(c => c.Id).ToList();
        var msgs = await _context.ChatMessages.Where(m => convIds.Contains(m.ConversationId)).ToListAsync();
        _context.ChatMessages.RemoveRange(msgs);
        _context.ChatConversations.RemoveRange(convs);

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Admin {AdminUser} removed product {ProductId} ({Title}) from marketplace",
            User.Identity?.Name, id, product.Title);

        TempData["SuccessMessage"] = "Product removed from the marketplace.";
        return RedirectToAction(nameof(Products));
    }

    // GET: /Admin/Categories (R.11.4)
    public async Task<IActionResult> Categories()
    {
        var categories = await _context.Categories
            .OrderBy(c => c.Name)
            .ToListAsync();

        var productCounts = await _context.Products
            .GroupBy(p => p.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

        ViewBag.ProductCounts = productCounts;
        return View(categories);
    }

    // POST: /Admin/AddCategory (R.11.1)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Category name is required.";
            return RedirectToAction(nameof(Categories));
        }

        var exists = await _context.Categories.AnyAsync(c => c.Name.ToLower() == name.Trim().ToLower());
        if (exists)
        {
            _logger.LogWarning("Admin category creation rejected: '{CategoryName}' already exists", name.Trim());
            TempData["ErrorMessage"] = "A category with this name already exists.";
            return RedirectToAction(nameof(Categories));
        }

        var category = new Category
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Admin {AdminUser} added category: {CategoryName}", User.Identity?.Name, category.Name);

        TempData["SuccessMessage"] = "Category added successfully!";
        return RedirectToAction(nameof(Categories));
    }

    // POST: /Admin/EditCategory (R.11.2)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(int id, string name, string? description)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Category name is required.";
            return RedirectToAction(nameof(Categories));
        }

        var exists = await _context.Categories
            .AnyAsync(c => c.Id != id && c.Name.ToLower() == name.Trim().ToLower());
        if (exists)
        {
            TempData["ErrorMessage"] = "Another category already exists with this name.";
            return RedirectToAction(nameof(Categories));
        }

        var oldName = category.Name;
        category.Name = name.Trim();
        category.Description = description?.Trim();
        await _context.SaveChangesAsync();

        _logger.LogInformation("Admin {AdminUser} updated category {CategoryId} from '{OldName}' to '{NewName}'",
            User.Identity?.Name, id, oldName, category.Name);

        TempData["SuccessMessage"] = "Category updated successfully!";
        return RedirectToAction(nameof(Categories));
    }

    // POST: /Admin/DeleteCategory/id (R.11.3)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();

        var associatedCount = await _context.Products.CountAsync(p => p.CategoryId == id);
        if (associatedCount > 0)
        {
            _logger.LogWarning("Admin {AdminUser} attempted to delete category {CategoryId} ({CategoryName}) with {Count} assigned products",
                User.Identity?.Name, id, category.Name, associatedCount);
            TempData["ErrorMessage"] = $"Cannot delete '{category.Name}' because {associatedCount} product(s) are currently listed under it.";
            return RedirectToAction(nameof(Categories));
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Admin {AdminUser} deleted category {CategoryId} ({CategoryName})",
            User.Identity?.Name, id, category.Name);

        TempData["SuccessMessage"] = "Category deleted successfully!";
        return RedirectToAction(nameof(Categories));
    }
}
