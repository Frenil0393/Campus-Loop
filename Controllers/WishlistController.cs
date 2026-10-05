using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusLoop.Data;
using CampusLoop.Models;
using CampusLoop.Services;
using CampusLoop.ViewModels;

namespace CampusLoop.Controllers;

[Authorize]
public class WishlistController : Controller
{
    private readonly CampusLoopDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMarketplaceItemService _marketplaceItemService;
    private readonly ILogger<WishlistController> _logger;

    public WishlistController(
        CampusLoopDbContext context,
        UserManager<ApplicationUser> userManager,
        IMarketplaceItemService marketplaceItemService,
        ILogger<WishlistController> logger)
    {
        _context = context;
        _userManager = userManager;
        _marketplaceItemService = marketplaceItemService;
        _logger = logger;
    }

    // GET: /Wishlist (R.5)
    public async Task<IActionResult> Index()
    {
        var currentUserId = _userManager.GetUserId(User)!;
        _logger.LogInformation("Wishlist viewed by User {UserId}", currentUserId);

        var wishlistItems = await _context.Wishlists
            .Where(w => w.StudentId == currentUserId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();

        var productIds = wishlistItems.Select(w => w.ProductId).ToList();

        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        var productDict = products.ToDictionary(p => p.Id);
        var orderedProducts = productIds
            .Where(id => productDict.ContainsKey(id))
            .Select(id => productDict[id])
            .ToList();

        var items = await _marketplaceItemService.BuildViewModelsAsync(orderedProducts, currentUserId);

        return View(items);
    }

    // POST: /Wishlist/Toggle (R.5)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int productId, string? returnUrl = null)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            _logger.LogWarning("Wishlist toggle requested for non-existent Product {ProductId}", productId);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                Request.Headers.Accept.ToString().Contains("application/json"))
            {
                return Json(new { success = false });
            }
            return NotFound();
        }

        var currentUserId = _userManager.GetUserId(User)!;

        var existing = await _context.Wishlists
            .FirstOrDefaultAsync(w => w.StudentId == currentUserId && w.ProductId == productId);

        bool added = false;
        if (existing != null)
        {
            _context.Wishlists.Remove(existing);
            await _context.SaveChangesAsync();
            added = false;
            _logger.LogInformation("Product {ProductId} removed from wishlist of User {UserId}", productId, currentUserId);
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
            _logger.LogInformation("Product {ProductId} added to wishlist of User {UserId}", productId, currentUserId);
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
