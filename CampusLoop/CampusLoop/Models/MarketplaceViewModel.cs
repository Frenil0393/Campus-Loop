namespace CampusLoop.Models;

public class MarketplaceViewModel
{
    public List<Product> Products { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public string? SelectedCategory { get; set; }
    public string? SearchKeyword { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? SortOrder { get; set; }
    
    // Quick statistics for the campus community
    public int TotalAvailableItems => Products.Count(p => p.Status == "AVAILABLE");
    public int TotalCategories => Categories.Count;
    public int ActiveStudentsCount { get; set; } = 480;
    public int SuccessfulDealsCount { get; set; } = 1250;
}
