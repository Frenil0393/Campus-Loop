using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusLoop.Data;
using CampusLoop.Models;

namespace CampusLoop.Controllers;

public class HomeController : Controller
{
    private readonly CampusLoopDbContext _context;

    public HomeController(CampusLoopDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? category, string? search, decimal? minPrice, decimal? maxPrice, string? sort)
    {
        // R.2.1: Retrieve available products from database, exclude SOLD
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Seller)
            .Where(p => p.Status == "AVAILABLE")
            .AsQueryable();

        // R.4.2: Browse by category
        if (!string.IsNullOrWhiteSpace(category) && category.ToLower() != "all")
        {
            query = query.Where(p => p.Category != null && p.Category.Slug.ToLower() == category.ToLower());
        }

        // R.4.1: Search product by keyword
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || 
                                     p.Description.ToLower().Contains(term) ||
                                     (p.Category != null && p.Category.Name.ToLower().Contains(term)));
        }

        // Price filtering
        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }
        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        // Sorting
        query = sort switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.Id)
        };

        var products = await query.ToListAsync();
        var categories = await _context.Categories.ToListAsync();

        var model = new MarketplaceViewModel
        {
            Products = products,
            Categories = categories,
            SelectedCategory = category ?? "all",
            SearchKeyword = search,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            SortOrder = sort ?? "newest",
            ActiveStudentsCount = await _context.Users.CountAsync(u => u.Role == "Student"),
            SuccessfulDealsCount = await _context.PaymentOrders.CountAsync() + 1250
        };

        return View(model);
    }

    // R.2.2 & R.14.2: JSON endpoint for Quick View modal
    [HttpGet]
    public async Task<IActionResult> QuickView(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Seller)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return NotFound(new { success = false, message = "Product not found" });
        }

        return Json(new
        {
            success = true,
            id = product.Id,
            name = product.Name,
            price = product.Price,
            description = product.Description,
            category = product.Category?.Name,
            condition = product.Condition,
            location = product.CampusLocation,
            status = product.Status,
            images = product.ImageUrls,
            seller = new
            {
                name = product.Seller?.FullName,
                branch = product.Seller?.Branch,
                semester = product.Seller?.Semester,
                email = product.Seller?.CollegeEmail,
                phone = product.Seller?.PhoneNumber,
                avatar = product.Seller?.AvatarUrl
            }
        });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
