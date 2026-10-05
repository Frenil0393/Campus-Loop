using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using CampusLoop.Controllers;
using CampusLoop.Models;
using CampusLoop.Services;
using CampusLoop.Tests.TestHelpers;
using CampusLoop.ViewModels;
using Xunit;

namespace CampusLoop.Tests.Controllers;

public class ProductOperationsTests
{
    private static (ProductsController controller, Mock<UserManager<ApplicationUser>> mockUserManager) CreateController(
        CampusLoop.Data.CampusLoopDbContext context,
        string currentUserId)
    {
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        var mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(currentUserId);
        mockUserManager.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => new ApplicationUser { Id = id, FullName = "Owner Student", Email = "owner@ddu.ac.in" });

        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());

        var mockMarketplaceService = new Mock<IMarketplaceItemService>();

        var controller = new ProductsController(
            context,
            mockUserManager.Object,
            mockEnv.Object,
            mockMarketplaceService.Object,
            new Mock<ILogger<ProductsController>>().Object);

        var httpContext = new DefaultHttpContext();
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, currentUserId)
        }, "mock"));

        httpContext.User = claimsPrincipal;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        return (controller, mockUserManager);
    }

    [Fact]
    public async Task Details_ReturnsNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var (controller, _) = CreateController(context, "user-1");

        // Act
        var result = await controller.Details(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_EnforcesMinimumThreeImagesRequirement()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        context.Categories.Add(new Category { Id = 1, Name = "Books" });
        await context.SaveChangesAsync();

        var (controller, _) = CreateController(context, "user-1");

        var model = new ProductCreateViewModel
        {
            Title = "Engineering Physics",
            Description = "Textbook in good condition",
            Price = 300,
            CategoryId = 1,
            ImageFiles = new List<IFormFile>() // 0 images (must be 3-4)
        };

        // Act
        var result = await controller.Create(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(ProductCreateViewModel.ImageFiles)));
    }

    [Fact]
    public async Task Delete_BlocksDeletion_WhenProductHasOrder()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var sellerId = "owner-1";
        var product = new Product
        {
            Id = 40,
            Title = "Sold Calculator",
            Price = 800,
            SellerId = sellerId,
            Status = ProductStatus.Sold,
            CreatedAt = DateTime.UtcNow
        };
        var order = new Order
        {
            Id = 1,
            ProductId = 40,
            BuyerId = "buyer-1",
            SellerId = sellerId,
            Amount = 800,
            TransactionId = "TXN-123",
            PaymentMethod = PaymentMethod.UpiQr,
            CreatedAt = DateTime.UtcNow
        };

        context.Products.Add(product);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var (controller, _) = CreateController(context, sellerId);

        // Act
        var result = await controller.Delete(40);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ProductsController.MyProducts), redirect.ActionName);
        Assert.Equal("This item has a purchase record and cannot be deleted.", controller.TempData["ErrorMessage"]);
        Assert.NotNull(await context.Products.FindAsync(40));
    }

    [Fact]
    public async Task Delete_ReturnsForbid_WhenCallerIsNotOwner()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var product = new Product
        {
            Id = 41,
            Title = "Drawing Drafter",
            Price = 450,
            SellerId = "owner-1",
            Status = ProductStatus.Available,
            CreatedAt = DateTime.UtcNow
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var (controller, _) = CreateController(context, currentUserId: "attacker-user");

        // Act
        var result = await controller.Delete(41);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }
}
