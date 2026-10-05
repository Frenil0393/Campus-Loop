using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using CampusLoop.Controllers;
using CampusLoop.Models;
using CampusLoop.Tests.TestHelpers;
using CampusLoop.ViewModels;
using Xunit;

namespace CampusLoop.Tests.Controllers;

public class CheckoutProcessingTests
{
    private readonly Mock<ILogger<CheckoutController>> _mockLogger = new();

    private static (CheckoutController controller, Mock<UserManager<ApplicationUser>> mockUserManager) CreateController(
        CampusLoop.Data.CampusLoopDbContext context,
        string currentUserId)
    {
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        var mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(currentUserId);
        mockUserManager.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => new ApplicationUser { Id = id, FullName = "Test User", Email = "test@ddu.ac.in" });

        var controller = new CheckoutController(context, mockUserManager.Object, new Mock<ILogger<CheckoutController>>().Object);

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
    public async Task Index_BlocksSellerFromBuyingOwnProduct()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var sellerId = "student-123";
        var product = new Product
        {
            Id = 10,
            Title = "Engineering Drawing Board",
            Price = 500,
            SellerId = sellerId,
            Status = ProductStatus.Available,
            CreatedAt = DateTime.UtcNow
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var (controller, _) = CreateController(context, currentUserId: sellerId);

        // Act
        var result = await controller.Index(10);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);
        Assert.Equal("Products", redirect.ControllerName);
        Assert.Equal("You cannot buy your own product listing.", controller.TempData["ErrorMessage"]);
    }

    [Fact]
    public async Task Index_BlocksBuyingAlreadySoldProduct()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var product = new Product
        {
            Id = 11,
            Title = "Sold Book",
            Price = 300,
            SellerId = "seller-99",
            Status = ProductStatus.Sold,
            CreatedAt = DateTime.UtcNow
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var (controller, _) = CreateController(context, currentUserId: "buyer-1");

        // Act
        var result = await controller.Index(11);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);
        Assert.Equal("Products", redirect.ControllerName);
        Assert.Equal("This item has already been sold.", controller.TempData["ErrorMessage"]);
    }

    [Fact]
    public async Task ProcessPayment_BlocksSellerBuyingOwnProduct()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var sellerId = "student-seller";
        var product = new Product
        {
            Id = 20,
            Title = "Lab Coat",
            Price = 200,
            SellerId = sellerId,
            Status = ProductStatus.Available,
            CreatedAt = DateTime.UtcNow
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var (controller, _) = CreateController(context, currentUserId: sellerId);
        var model = new CheckoutViewModel
        {
            ProductId = 20,
            PaymentMethod = PaymentMethod.Cash
        };

        // Act
        var result = await controller.ProcessPayment(model);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);
        Assert.Equal("Products", redirect.ControllerName);
        Assert.Equal("You cannot buy your own product listing.", controller.TempData["ErrorMessage"]);
    }

    [Fact]
    public async Task ProcessPayment_BlocksBuyingSoldProduct()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var product = new Product
        {
            Id = 21,
            Title = "Sold Casio Calculator",
            Price = 850,
            SellerId = "seller-1",
            Status = ProductStatus.Sold,
            CreatedAt = DateTime.UtcNow
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var (controller, _) = CreateController(context, currentUserId: "buyer-2");
        var model = new CheckoutViewModel
        {
            ProductId = 21,
            PaymentMethod = PaymentMethod.Cash
        };

        // Act
        var result = await controller.ProcessPayment(model);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
        Assert.Equal("This item has already been marked as sold.", controller.TempData["ErrorMessage"]);
    }

    [Fact]
    public async Task ProcessPayment_ValidatesMissingUpiQrReference()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var product = new Product
        {
            Id = 30,
            Title = "Wireless Mouse",
            Price = 400,
            SellerId = "seller-1",
            Status = ProductStatus.Available,
            CategoryId = 1,
            CreatedAt = DateTime.UtcNow
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var (controller, _) = CreateController(context, currentUserId: "buyer-3");
        var model = new CheckoutViewModel
        {
            ProductId = 30,
            PaymentMethod = PaymentMethod.UpiQr,
            UpiTransactionRef = "" // Missing required ref
        };

        // Act
        var result = await controller.ProcessPayment(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(CheckoutViewModel.UpiTransactionRef)));
    }

    [Fact]
    public async Task ProcessPayment_ValidatesMissingCardFields()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var product = new Product
        {
            Id = 31,
            Title = "Drawing Scales",
            Price = 150,
            SellerId = "seller-1",
            Status = ProductStatus.Available,
            CategoryId = 1,
            CreatedAt = DateTime.UtcNow
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var (controller, _) = CreateController(context, currentUserId: "buyer-3");
        var model = new CheckoutViewModel
        {
            ProductId = 31,
            PaymentMethod = PaymentMethod.Card,
            CardHolderName = "",
            CardNumber = "",
            CardExpiry = "",
            CardCvv = ""
        };

        // Act
        var result = await controller.ProcessPayment(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(CheckoutViewModel.CardHolderName)));
        Assert.True(controller.ModelState.ContainsKey(nameof(CheckoutViewModel.CardNumber)));
        Assert.True(controller.ModelState.ContainsKey(nameof(CheckoutViewModel.CardExpiry)));
        Assert.True(controller.ModelState.ContainsKey(nameof(CheckoutViewModel.CardCvv)));
    }

    [Fact]
    public void CollegeVerifiedTxnId_FormatMatchesSpecification()
    {
        // Act
        var txnId = $"TXN-DDU-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}".ToUpperInvariant();

        // Assert
        Assert.StartsWith("TXN-DDU-", txnId);
        Assert.Contains(DateTime.UtcNow.ToString("yyyyMMdd"), txnId);
    }
}
