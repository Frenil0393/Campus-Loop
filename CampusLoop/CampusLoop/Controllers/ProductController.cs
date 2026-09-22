using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusLoop.Data;
using CampusLoop.Models;

namespace CampusLoop.Controllers;

public class ProductController : Controller
{
    private readonly CampusLoopDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public ProductController(CampusLoopDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // ==========================================
    // R.2.2 VIEW PRODUCT DETAILS
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Seller)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return NotFound("Product not found");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            ViewBag.IsWishlisted = await _context.WishlistItems
                .AnyAsync(w => w.UserId == userId && w.ProductId == id);
            ViewBag.IsOwner = (product.SellerId == userId);
        }
        else
        {
            ViewBag.IsWishlisted = false;
            ViewBag.IsOwner = false;
        }

        return View(product);
    }

    // ==========================================
    // R.3.1 POST PRODUCT (3-4 Images)
    // ==========================================
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await _context.Categories.Where(c => c.Slug != "all").ToListAsync();
        return View();
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string name, 
        int categoryId, 
        decimal price, 
        string condition, 
        string campusLocation, 
        string description,
        List<IFormFile> imageFiles)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

        // Validate required fields
        if (string.IsNullOrWhiteSpace(name) || categoryId <= 0 || price <= 0 || string.IsNullOrWhiteSpace(description))
        {
            ModelState.AddModelError("", "Please fill in all required product fields.");
            ViewBag.Categories = await _context.Categories.Where(c => c.Slug != "all").ToListAsync();
            return View();
        }

        var imageUrls = new List<string>();

        // Handle uploaded image files (R.14.1: Store in application storage)
        if (imageFiles != null && imageFiles.Count > 0)
        {
            var uploadDir = Path.Combine(_environment.WebRootPath, "uploads", "products");
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            foreach (var file in imageFiles.Take(4))
            {
                if (file.Length > 0)
                {
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(ext))
                    {
                        var uniqueFileName = $"{Guid.NewGuid():N}{ext}";
                        var filePath = Path.Combine(uploadDir, uniqueFileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        imageUrls.Add($"/uploads/products/{uniqueFileName}");
                    }
                }
            }
        }

        // Fallback default high-quality image if user uploaded less than required
        if (imageUrls.Count == 0)
        {
            imageUrls.Add("https://images.unsplash.com/photo-1581092160607-ee22621dd758?w=600");
            imageUrls.Add("https://images.unsplash.com/photo-1587145820266-a5951ee6f620?w=600");
            imageUrls.Add("https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=600");
        }

        var product = new Product
        {
            Name = name.Trim(),
            CategoryId = categoryId,
            Price = price,
            Condition = condition ?? "Good",
            CampusLocation = campusLocation ?? "Campus Library Lawn",
            Description = description.Trim(),
            SellerId = userId,
            Status = "AVAILABLE", // R.3.1: Set initial status as AVAILABLE
            ImageUrls = imageUrls,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"🎉 '{product.Name}' was successfully posted to the CampusLoop marketplace!";
        return RedirectToAction("Details", new { id = product.Id });
    }

    // ==========================================
    // R.3.2 VIEW MY PRODUCTS
    // ==========================================
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> MyProducts()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var products = await _context.Products
            .Include(p => p.Category)
            .Where(p => p.SellerId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return View(products);
    }

    // ==========================================
    // R.3.3 EDIT PRODUCT
    // ==========================================
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var product = await _context.Products.FindAsync(id);

        if (product == null) return NotFound();

        // R.13.3 Product Ownership Validation
        if (product.SellerId != userId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        ViewBag.Categories = await _context.Categories.Where(c => c.Slug != "all").ToListAsync();
        return View(product);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string name, int categoryId, decimal price, string condition, string campusLocation, string description, string status)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var product = await _context.Products.FindAsync(id);

        if (product == null) return NotFound();

        // R.13.3 Product Ownership Validation
        if (product.SellerId != userId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        product.Name = name.Trim();
        product.CategoryId = categoryId;
        product.Price = price;
        product.Condition = condition;
        product.CampusLocation = campusLocation;
        product.Description = description.Trim();
        if (!string.IsNullOrEmpty(status))
        {
            product.Status = status;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Product details updated successfully!";
        return RedirectToAction(nameof(MyProducts));
    }

    // ==========================================
    // R.3.4 DELETE PRODUCT
    // ==========================================
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var product = await _context.Products.FindAsync(id);

        if (product == null) return NotFound();

        // R.13.3 Product Ownership Validation
        if (product.SellerId != userId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Product removed from marketplace.";
        return RedirectToAction(nameof(MyProducts));
    }

    // ==========================================
    // R.3.5 MARK PRODUCT AS SOLD
    // ==========================================
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkSold(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var product = await _context.Products.FindAsync(id);

        if (product == null) return NotFound();

        // R.13.3 Product Ownership Validation
        if (product.SellerId != userId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        product.Status = "SOLD";
        await _context.SaveChangesAsync();

        TempData["Success"] = $"'{product.Name}' is now marked as SOLD! It has been archived from search results.";
        return RedirectToAction(nameof(MyProducts));
    }

    // ==========================================
    // WISHLIST MANAGEMENT
    // ==========================================
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ToggleWishlist(int productId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var existing = await _context.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

        bool isWishlisted;
        if (existing != null)
        {
            _context.WishlistItems.Remove(existing);
            isWishlisted = false;
        }
        else
        {
            _context.WishlistItems.Add(new WishlistItem
            {
                UserId = userId,
                ProductId = productId,
                AddedAt = DateTime.UtcNow
            });
            isWishlisted = true;
        }

        await _context.SaveChangesAsync();
        var totalCount = await _context.WishlistItems.CountAsync(w => w.UserId == userId);

        return Json(new { success = true, isWishlisted, totalCount });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Wishlist()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var items = await _context.WishlistItems
            .Include(w => w.Product)
                .ThenInclude(p => p!.Category)
            .Include(w => w.Product)
                .ThenInclude(p => p!.Seller)
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.AddedAt)
            .ToListAsync();

        return View(items);
    }
}
