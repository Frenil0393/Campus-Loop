using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CampusLoop.Data;
using CampusLoop.Models;
using CampusLoop.ViewModels;

namespace CampusLoop.Services;

public class MarketplaceItemService : IMarketplaceItemService
{
    private readonly CampusLoopDbContext _context;
    private readonly ILogger<MarketplaceItemService> _logger;

    public MarketplaceItemService(CampusLoopDbContext context, ILogger<MarketplaceItemService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<MarketplaceItemViewModel>> GetMarketplaceItemsAsync(string? search, int? categoryId, string? sort, string? currentUserId = null)
    {
        _logger.LogInformation("Querying marketplace items. Search: {Search}, CategoryId: {CategoryId}, Sort: {Sort}, CurrentUserId: {UserId}",
            search, categoryId, sort, currentUserId);

        var query = _context.Products.Where(p => p.Status == ProductStatus.Available);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Title.ToLower().Contains(term) || p.Description.ToLower().Contains(term));
        }

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        query = sort switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "oldest" => query.OrderBy(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var products = await query.ToListAsync();
        return await BuildViewModelsAsync(products, currentUserId);
    }

    public async Task<List<MarketplaceItemViewModel>> BuildViewModelsAsync(IEnumerable<Product> products, string? currentUserId = null)
    {
        var productList = products as IList<Product> ?? products.ToList();
        if (!productList.Any())
        {
            return new List<MarketplaceItemViewModel>();
        }

        var productIds = productList.Select(p => p.Id).ToList();

        var images = await _context.ProductImages
            .Where(img => productIds.Contains(img.ProductId))
            .ToListAsync();

        var categories = await _context.Categories.ToDictionaryAsync(c => c.Id, c => c.Name);

        var sellerIds = productList.Select(p => p.SellerId).Distinct().ToList();
        var sellers = await _context.Users
            .Where(u => sellerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);

        var wishlistProductIds = new HashSet<int>();
        if (!string.IsNullOrEmpty(currentUserId))
        {
            wishlistProductIds = (await _context.Wishlists
                .Where(w => w.StudentId == currentUserId && productIds.Contains(w.ProductId))
                .Select(w => w.ProductId)
                .ToListAsync())
                .ToHashSet();
        }

        return productList.Select(p => new MarketplaceItemViewModel
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
    }
}
