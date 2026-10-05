using Microsoft.Extensions.Logging;
using Moq;
using CampusLoop.Models;
using CampusLoop.Services;
using CampusLoop.Tests.TestHelpers;
using Xunit;

namespace CampusLoop.Tests.Services;

public class MarketplaceItemServiceTests
{
    private readonly Mock<ILogger<MarketplaceItemService>> _mockLogger = new();

    [Fact]
    public async Task GetMarketplaceItemsAsync_ReturnsOnlyAvailableProducts()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var cat = new Category { Id = 1, Name = "Electronics" };
        var seller = new ApplicationUser { Id = "seller-1", UserName = "seller@ddu.ac.in", FullName = "Seller Student" };
        context.Categories.Add(cat);
        context.Users.Add(seller);

        context.Products.AddRange(
            new Product { Id = 1, Title = "Available Item", CategoryId = 1, SellerId = "seller-1", Price = 500, Status = ProductStatus.Available, CreatedAt = DateTime.UtcNow },
            new Product { Id = 2, Title = "Sold Item", CategoryId = 1, SellerId = "seller-1", Price = 300, Status = ProductStatus.Sold, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new MarketplaceItemService(context, _mockLogger.Object);

        // Act
        var result = await service.GetMarketplaceItemsAsync(search: null, categoryId: null, sort: null);

        // Assert
        Assert.Single(result);
        Assert.Equal("Available Item", result[0].Title);
        Assert.Equal(ProductStatus.Available, result[0].Status);
    }

    [Fact]
    public async Task GetMarketplaceItemsAsync_FiltersByKeywordInTitleOrDescription()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var cat = new Category { Id = 1, Name = "Books" };
        var seller = new ApplicationUser { Id = "seller-1", UserName = "seller@ddu.ac.in", FullName = "Seller Student" };
        context.Categories.Add(cat);
        context.Users.Add(seller);

        context.Products.AddRange(
            new Product { Id = 1, Title = "Calculus Textbook", Description = "Clean pages", CategoryId = 1, SellerId = "seller-1", Price = 250, Status = ProductStatus.Available, CreatedAt = DateTime.UtcNow },
            new Product { Id = 2, Title = "Physics Notes", Description = "Comprehensive calculus review included", CategoryId = 1, SellerId = "seller-1", Price = 150, Status = ProductStatus.Available, CreatedAt = DateTime.UtcNow },
            new Product { Id = 3, Title = "Chemistry Lab Manual", Description = "Organic chemistry experiments", CategoryId = 1, SellerId = "seller-1", Price = 120, Status = ProductStatus.Available, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new MarketplaceItemService(context, _mockLogger.Object);

        // Act
        var result = await service.GetMarketplaceItemsAsync(search: "calculus", categoryId: null, sort: null);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Title == "Calculus Textbook");
        Assert.Contains(result, p => p.Title == "Physics Notes");
        Assert.DoesNotContain(result, p => p.Title == "Chemistry Lab Manual");
    }

    [Fact]
    public async Task GetMarketplaceItemsAsync_FiltersByCategoryId()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var cat1 = new Category { Id = 1, Name = "Calculators" };
        var cat2 = new Category { Id = 2, Name = "Furniture" };
        var seller = new ApplicationUser { Id = "seller-1", UserName = "seller@ddu.ac.in", FullName = "Seller Student" };
        context.Categories.AddRange(cat1, cat2);
        context.Users.Add(seller);

        context.Products.AddRange(
            new Product { Id = 1, Title = "Casio Scientific Calculator", CategoryId = 1, SellerId = "seller-1", Price = 800, Status = ProductStatus.Available, CreatedAt = DateTime.UtcNow },
            new Product { Id = 2, Title = "Study Table", CategoryId = 2, SellerId = "seller-1", Price = 600, Status = ProductStatus.Available, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new MarketplaceItemService(context, _mockLogger.Object);

        // Act
        var result = await service.GetMarketplaceItemsAsync(search: null, categoryId: 1, sort: null);

        // Assert
        Assert.Single(result);
        Assert.Equal("Casio Scientific Calculator", result[0].Title);
        Assert.Equal("Calculators", result[0].CategoryName);
    }

    [Fact]
    public async Task GetMarketplaceItemsAsync_SortsByPriceAscendingAndDescending()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var cat = new Category { Id = 1, Name = "Electronics" };
        var seller = new ApplicationUser { Id = "seller-1", UserName = "seller@ddu.ac.in", FullName = "Seller Student" };
        context.Categories.Add(cat);
        context.Users.Add(seller);

        context.Products.AddRange(
            new Product { Id = 1, Title = "Mid Item", Price = 500, CategoryId = 1, SellerId = "seller-1", Status = ProductStatus.Available, CreatedAt = DateTime.UtcNow },
            new Product { Id = 2, Title = "Low Item", Price = 100, CategoryId = 1, SellerId = "seller-1", Status = ProductStatus.Available, CreatedAt = DateTime.UtcNow },
            new Product { Id = 3, Title = "High Item", Price = 1000, CategoryId = 1, SellerId = "seller-1", Status = ProductStatus.Available, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new MarketplaceItemService(context, _mockLogger.Object);

        // Act - Ascending
        var ascResult = await service.GetMarketplaceItemsAsync(search: null, categoryId: null, sort: "price_asc");
        // Act - Descending
        var descResult = await service.GetMarketplaceItemsAsync(search: null, categoryId: null, sort: "price_desc");

        // Assert
        Assert.Equal(100, ascResult[0].Price);
        Assert.Equal(500, ascResult[1].Price);
        Assert.Equal(1000, ascResult[2].Price);

        Assert.Equal(1000, descResult[0].Price);
        Assert.Equal(500, descResult[1].Price);
        Assert.Equal(100, descResult[2].Price);
    }

    [Fact]
    public async Task GetMarketplaceItemsAsync_SortsByOldestAndNewest()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var cat = new Category { Id = 1, Name = "Books" };
        var seller = new ApplicationUser { Id = "seller-1", UserName = "seller@ddu.ac.in", FullName = "Seller Student" };
        context.Categories.Add(cat);
        context.Users.Add(seller);

        var time = DateTime.UtcNow;
        context.Products.AddRange(
            new Product { Id = 1, Title = "Older Item", Price = 100, CategoryId = 1, SellerId = "seller-1", Status = ProductStatus.Available, CreatedAt = time.AddDays(-5) },
            new Product { Id = 2, Title = "Newer Item", Price = 200, CategoryId = 1, SellerId = "seller-1", Status = ProductStatus.Available, CreatedAt = time }
        );
        await context.SaveChangesAsync();

        var service = new MarketplaceItemService(context, _mockLogger.Object);

        // Act
        var oldestFirst = await service.GetMarketplaceItemsAsync(null, null, "oldest");
        var newestFirst = await service.GetMarketplaceItemsAsync(null, null, null);

        // Assert
        Assert.Equal("Older Item", oldestFirst[0].Title);
        Assert.Equal("Newer Item", newestFirst[0].Title);
    }

    [Fact]
    public async Task BuildViewModelsAsync_SetsIsInWishlistTrue_WhenUserAddedProductToWishlist()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var cat = new Category { Id = 1, Name = "Equipment" };
        var seller = new ApplicationUser { Id = "seller-1", UserName = "seller@ddu.ac.in", FullName = "Seller Student" };
        var buyer = new ApplicationUser { Id = "buyer-1", UserName = "buyer@ddu.ac.in", FullName = "Buyer Student" };
        context.Categories.Add(cat);
        context.Users.AddRange(seller, buyer);

        var product1 = new Product { Id = 1, Title = "Mini Drafter", CategoryId = 1, SellerId = "seller-1", Price = 450, Status = ProductStatus.Available, CreatedAt = DateTime.UtcNow };
        var product2 = new Product { Id = 2, Title = "Lab Coat", CategoryId = 1, SellerId = "seller-1", Price = 200, Status = ProductStatus.Available, CreatedAt = DateTime.UtcNow };
        context.Products.AddRange(product1, product2);

        // Buyer has product 1 in wishlist
        context.Wishlists.Add(new Wishlist { Id = 1, StudentId = "buyer-1", ProductId = 1, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var service = new MarketplaceItemService(context, _mockLogger.Object);

        // Act
        var vms = await service.BuildViewModelsAsync(new[] { product1, product2 }, currentUserId: "buyer-1");

        // Assert
        var item1 = vms.First(v => v.Id == 1);
        var item2 = vms.First(v => v.Id == 2);
        Assert.True(item1.IsInWishlist);
        Assert.False(item2.IsInWishlist);
    }

    [Fact]
    public async Task BuildViewModelsAsync_ReturnsEmptyList_WhenNoProductsProvided()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var service = new MarketplaceItemService(context, _mockLogger.Object);

        // Act
        var result = await service.BuildViewModelsAsync(Enumerable.Empty<Product>());

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
