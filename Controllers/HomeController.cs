using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusLoop.Data;
using CampusLoop.Models;
using CampusLoop.ViewModels;

namespace CampusLoop.Controllers;

public class HomeController : Controller
{
    private readonly CampusLoopDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(CampusLoopDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: / (Marketplace - R.2.1, R.4.1, R.4.2)
    public async Task<IActionResult> Index(string? search, int? categoryId, string? sort)
    {
        var currentUserId = _userManager.GetUserId(User);

        // Query available products
        var query = _context.Products
            .Where(p => p.Status == ProductStatus.Available);

        // Search keyword filter (R.4.1)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Title.ToLower().Contains(term) || p.Description.ToLower().Contains(term));
        }

        // Category filter (R.4.2)
        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        // Sorting
        query = sort switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "oldest" => query.OrderBy(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt) // Default: newest
        };

        var products = await query.ToListAsync();
        var productIds = products.Select(p => p.Id).ToList();

        // Load primary images
        var images = await _context.ProductImages
            .Where(img => productIds.Contains(img.ProductId))
            .ToListAsync();

        // Load categories dictionary
        var categories = await _context.Categories.ToDictionaryAsync(c => c.Id, c => c.Name);

        // Load seller names dictionary
        var sellerIds = products.Select(p => p.SellerId).Distinct().ToList();
        var sellers = await _context.Users
            .Where(u => sellerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);

        // Load user's wishlist IDs
        var wishlistProductIds = new HashSet<int>();
        if (!string.IsNullOrEmpty(currentUserId))
        {
            wishlistProductIds = (await _context.Wishlists
                .Where(w => w.StudentId == currentUserId)
                .Select(w => w.ProductId)
                .ToListAsync())
                .ToHashSet();
        }

        var items = products.Select(p => new MarketplaceItemViewModel
        {
            Id = p.Id,
            Title = p.Title,
            Price = p.Price,
            CategoryName = categories.TryGetValue(p.CategoryId, out var catName) ? catName : "General",
            PrimaryImageUrl = images.FirstOrDefault(i => i.ProductId == p.Id && i.IsPrimary)?.ImageUrl 
                ?? images.FirstOrDefault(i => i.ProductId == p.Id)?.ImageUrl 
                ?? "/images/placeholder.png",
            SellerName = sellers.TryGetValue(p.SellerId, out var sName) ? sName : "Student",
            Status = p.Status,
            CreatedAt = p.CreatedAt,
            IsInWishlist = wishlistProductIds.Contains(p.Id)
        }).ToList();

        ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        ViewBag.CurrentSearch = search;
        ViewBag.CurrentCategory = categoryId;
        ViewBag.CurrentSort = sort;

        return View(items);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
