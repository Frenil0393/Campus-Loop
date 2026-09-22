using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusLoop.Data;
using CampusLoop.Models;
using CampusLoop.ViewModels;

namespace CampusLoop.Controllers;

[Authorize]
public class WishlistController : Controller
{
    private readonly CampusLoopDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public WishlistController(CampusLoopDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /Wishlist (R.5)
    public async Task<IActionResult> Index()
    {
        var currentUserId = _userManager.GetUserId(User)!;

        var wishlistItems = await _context.Wishlists
            .Where(w => w.StudentId == currentUserId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();

        var productIds = wishlistItems.Select(w => w.ProductId).ToList();

        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        var images = await _context.ProductImages
            .Where(img => productIds.Contains(img.ProductId))
            .ToListAsync();

        var categories = await _context.Categories.ToDictionaryAsync(c => c.Id, c => c.Name);
        var sellerIds = products.Select(p => p.SellerId).Distinct().ToList();
        var sellers = await _context.Users
            .Where(u => sellerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);

        var items = products.Select(p => new MarketplaceItemViewModel
        {
            Id = p.Id,
            Title = p.Title,
            Price = p.Price,
            CategoryName = categories.TryGetValue(p.CategoryId, out var cName) ? cName : "General",
            PrimaryImageUrl = images.FirstOrDefault(i => i.ProductId == p.Id && i.IsPrimary)?.ImageUrl
                ?? images.FirstOrDefault(i => i.ProductId == p.Id)?.ImageUrl
                ?? "/images/placeholder.png",
            SellerName = sellers.TryGetValue(p.SellerId, out var sName) ? sName : "Student",
            Status = p.Status,
            CreatedAt = p.CreatedAt,
            IsInWishlist = true
        }).ToList();

        return View(items);
    }

    // POST: /Wishlist/Toggle (R.5)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int productId, string? returnUrl = null)
    {
        var currentUserId = _userManager.GetUserId(User)!;

        var existing = await _context.Wishlists
            .FirstOrDefaultAsync(w => w.StudentId == currentUserId && w.ProductId == productId);

        bool added = false;
        if (existing != null)
        {
            _context.Wishlists.Remove(existing);
            await _context.SaveChangesAsync();
            added = false;
            TempData["SuccessMessage"] = "Item removed from your wishlist.";
        }
        else
        {
            _context.Wishlists.Add(new Wishlist
            {
                StudentId = currentUserId,
                ProductId = productId,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            added = true;
            TempData["SuccessMessage"] = "Item added to your wishlist!";
        }

        // If AJAX request
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = true, added = added });
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }
}
