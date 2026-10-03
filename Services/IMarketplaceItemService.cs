using CampusLoop.Models;
using CampusLoop.ViewModels;

namespace CampusLoop.Services;

public interface IMarketplaceItemService
{
    Task<List<MarketplaceItemViewModel>> BuildViewModelsAsync(IEnumerable<Product> products, string? currentUserId = null);
    Task<List<MarketplaceItemViewModel>> GetMarketplaceItemsAsync(string? search, int? categoryId, string? sort, string? currentUserId = null);
}
