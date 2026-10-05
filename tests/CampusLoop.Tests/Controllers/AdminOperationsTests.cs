using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using CampusLoop.Controllers;
using CampusLoop.Models;
using CampusLoop.Tests.TestHelpers;
using Xunit;

namespace CampusLoop.Tests.Controllers;

public class AdminOperationsTests
{
    private static (AdminController controller, Mock<UserManager<ApplicationUser>> mockUserManager) CreateController(
        CampusLoop.Data.CampusLoopDbContext context)
    {
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        var mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var controller = new AdminController(context, mockUserManager.Object, new Mock<ILogger<AdminController>>().Object);

        var httpContext = new DefaultHttpContext();
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "AdminUser"),
            new Claim(ClaimTypes.Role, Roles.Admin)
        }, "mock"));

        httpContext.User = claimsPrincipal;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        return (controller, mockUserManager);
    }

    [Fact]
    public async Task AddCategory_RejectsEmptyOrWhitespaceName()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var (controller, _) = CreateController(context);

        // Act
        var result = await controller.AddCategory("   ", "Description");

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminController.Categories), redirect.ActionName);
        Assert.Equal("Category name is required.", controller.TempData["ErrorMessage"]);
        Assert.Empty(context.Categories);
    }

    [Fact]
    public async Task AddCategory_RejectsDuplicateCategoryNameCaseInsensitively()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        context.Categories.Add(new Category { Id = 1, Name = "Books", CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var (controller, _) = CreateController(context);

        // Act
        var result = await controller.AddCategory("books", "New books");

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminController.Categories), redirect.ActionName);
        Assert.Equal("A category with this name already exists.", controller.TempData["ErrorMessage"]);
        Assert.Single(context.Categories);
    }

    [Fact]
    public async Task AddCategory_SuccessfullyAddsNewCategory()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var (controller, _) = CreateController(context);

        // Act
        var result = await controller.AddCategory("Lab Coats", "White laboratory aprons");

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminController.Categories), redirect.ActionName);
        Assert.Equal("Category added successfully!", controller.TempData["SuccessMessage"]);

        var category = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Lab Coats");
        Assert.NotNull(category);
        Assert.Equal("White laboratory aprons", category.Description);
    }

    [Fact]
    public async Task DeleteCategory_BlocksDeletion_WhenProductsAreAssigned()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var category = new Category { Id = 5, Name = "Calculators", CreatedAt = DateTime.UtcNow };
        context.Categories.Add(category);
        context.Products.Add(new Product
        {
            Id = 1,
            Title = "Casio fx-991EX",
            CategoryId = 5,
            SellerId = "student-1",
            Price = 900,
            Status = ProductStatus.Available,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var (controller, _) = CreateController(context);

        // Act
        var result = await controller.DeleteCategory(5);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminController.Categories), redirect.ActionName);
        Assert.Contains("Cannot delete 'Calculators' because 1 product(s) are currently listed under it.", (string)controller.TempData["ErrorMessage"]!);
        Assert.NotNull(await context.Categories.FindAsync(5));
    }

    [Fact]
    public async Task DeleteCategory_AllowsDeletion_WhenNoProductsAreAssigned()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var category = new Category { Id = 6, Name = "Empty Category", CreatedAt = DateTime.UtcNow };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var (controller, _) = CreateController(context);

        // Act
        var result = await controller.DeleteCategory(6);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminController.Categories), redirect.ActionName);
        Assert.Equal("Category deleted successfully!", controller.TempData["SuccessMessage"]);
        Assert.Null(await context.Categories.FindAsync(6));
    }

    [Fact]
    public async Task ToggleStudentStatus_InvertsIsActiveFlag()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var student = new ApplicationUser
        {
            Id = "student-toggle",
            UserName = "student@ddu.ac.in",
            Email = "student@ddu.ac.in",
            FullName = "Student Test",
            IsActive = true
        };

        var (controller, mockUserManager) = CreateController(context);
        mockUserManager.Setup(m => m.FindByIdAsync("student-toggle")).ReturnsAsync(student);
        mockUserManager.Setup(m => m.UpdateAsync(student)).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await controller.ToggleStudentStatus("student-toggle");

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminController.Students), redirect.ActionName);
        Assert.False(student.IsActive);
        Assert.Equal("Student account deactivated.", controller.TempData["SuccessMessage"]);
        mockUserManager.Verify(m => m.UpdateAsync(student), Times.Once);
    }
}
