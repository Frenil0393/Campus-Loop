using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CampusLoop.Data;
using CampusLoop.Models;
using CampusLoop.Services;
using CampusLoop.ViewModels;

namespace CampusLoop.Controllers;

[Authorize]
public class ProductsController : Controller
{
    private readonly CampusLoopDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;
    private readonly IMarketplaceItemService _marketplaceItemService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        CampusLoopDbContext context,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment,
        IMarketplaceItemService marketplaceItemService,
        ILogger<ProductsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
        _marketplaceItemService = marketplaceItemService;
        _logger = logger;
    }

    // GET: /Products/Details/5 (R.2.2, R.7.1, R.14.2)
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            _logger.LogWarning("Product details request for non-existent id: {ProductId}", id);
            return NotFound();
        }

        _logger.LogInformation("Product details viewed: {ProductId} ({Title})", product.Id, product.Title);

        var category = await _context.Categories.FindAsync(product.CategoryId);
        var seller = await _userManager.FindByIdAsync(product.SellerId);
        var images = await _context.ProductImages
            .Where(img => img.ProductId == id)
            .OrderByDescending(img => img.IsPrimary)
            .Select(img => img.ImageUrl)
            .ToListAsync();

        var currentUserId = _userManager.GetUserId(User);
        bool isInWishlist = false;
        if (!string.IsNullOrEmpty(currentUserId))
        {
            isInWishlist = await _context.Wishlists
                .AnyAsync(w => w.StudentId == currentUserId && w.ProductId == id);
        }

        var vm = new ProductDetailsViewModel
        {
            Id = product.Id,
            Title = product.Title,
            Description = product.Description,
            Price = product.Price,
            CategoryName = category?.Name ?? "General",
            Status = product.Status,
            CreatedAt = product.CreatedAt,
            ImageUrls = images,
            SellerId = product.SellerId,
            SellerName = seller?.FullName ?? "Unknown Student",
            SellerEmail = seller?.Email ?? "",
            SellerPhone = seller?.PhoneNumber ?? "",
            SellerBranch = seller?.Branch ?? "",
            SellerSemester = seller?.Semester ?? 1,
            IsInWishlist = isInWishlist,
            IsOwner = !string.IsNullOrEmpty(currentUserId) && currentUserId == product.SellerId
        };

        return View(vm);
    }

    // GET: /Products/Create (R.3.1, R.14.1)
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
        return View();
    }

    // POST: /Products/Create (R.3.1, R.14.1)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductCreateViewModel model)
    {
        // Enforce 3-4 images requirement from R.3.1 and R.14.1
        if (model.ImageFiles == null || model.ImageFiles.Count < 3 || model.ImageFiles.Count > 4)
        {
            _logger.LogWarning("Product creation rejected: invalid image count ({Count})", model.ImageFiles?.Count ?? 0);
            ModelState.AddModelError("ImageFiles", "Please upload between 3 and 4 images for the product.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", model.CategoryId);
            return View(model);
        }

        var currentUserId = _userManager.GetUserId(User)!;

        var product = new Product
        {
            Title = model.Title,
            Description = model.Description,
            Price = model.Price,
            CategoryId = model.CategoryId,
            SellerId = currentUserId,
            Status = ProductStatus.Available,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Save uploaded images
        var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads", "products");
        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }

        for (int i = 0; i < model.ImageFiles!.Count; i++)
        {
            var file = model.ImageFiles[i];
            var extension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsDir, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var productImage = new ProductImage
            {
                ProductId = product.Id,
                ImageUrl = $"/uploads/products/{uniqueFileName}",
                IsPrimary = (i == 0),
                CreatedAt = DateTime.UtcNow
            };

            _context.ProductImages.Add(productImage);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Product created successfully: {ProductId} ({Title}) by Seller {SellerId}",
            product.Id, product.Title, currentUserId);

        TempData["SuccessMessage"] = "Product posted successfully in the marketplace!";
        return RedirectToAction(nameof(MyProducts));
    }

    // GET: /Products/MyProducts (R.3.2)
    public async Task<IActionResult> MyProducts()
    {
        var currentUserId = _userManager.GetUserId(User)!;

        var products = await _context.Products
            .Where(p => p.SellerId == currentUserId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var items = await _marketplaceItemService.BuildViewModelsAsync(products, currentUserId);

        return View(items);
    }

    // GET: /Products/Edit/5 (R.3.3, R.13.3)
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var currentUserId = _userManager.GetUserId(User)!;
        var product = await _context.Products.FindAsync(id);

        if (product == null) return NotFound();
        if (product.SellerId != currentUserId)
        {
            _logger.LogWarning("Forbidden edit attempt: User {UserId} tried to access edit page for Product {ProductId} owned by {SellerId}",
                currentUserId, id, product.SellerId);
            return Forbid(); // Ownership check (R.13.3)
        }

        var existingImages = await _context.ProductImages
            .Where(img => img.ProductId == id)
            .Select(img => img.ImageUrl)
            .ToListAsync();

        var vm = new ProductEditViewModel
        {
            Id = product.Id,
            Title = product.Title,
            CategoryId = product.CategoryId,
            Price = product.Price,
            Description = product.Description,
            ExistingImageUrls = existingImages
        };

        ViewBag.Categories = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", product.CategoryId);
        return View(vm);
    }

    // POST: /Products/Edit/5 (R.3.3, R.13.3)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductEditViewModel model)
    {
        var currentUserId = _userManager.GetUserId(User)!;
        var product = await _context.Products.FindAsync(model.Id);

        if (product == null) return NotFound();
        if (product.SellerId != currentUserId)
        {
            _logger.LogWarning("Forbidden edit submission: User {UserId} tried to edit Product {ProductId} owned by {SellerId}",
                currentUserId, model.Id, product.SellerId);
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", model.CategoryId);
            return View(model);
        }

        product.Title = model.Title;
        product.CategoryId = model.CategoryId;
        product.Price = model.Price;
        product.Description = model.Description;
        product.UpdatedAt = DateTime.UtcNow;

        // If new images provided, upload and replace
        if (model.NewImageFiles != null && model.NewImageFiles.Count >= 3 && model.NewImageFiles.Count <= 4)
        {
            var oldImages = await _context.ProductImages.Where(i => i.ProductId == model.Id).ToListAsync();
            _context.ProductImages.RemoveRange(oldImages);

            var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads", "products");
            for (int i = 0; i < model.NewImageFiles.Count; i++)
            {
                var file = model.NewImageFiles[i];
                var extension = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsDir, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _context.ProductImages.Add(new ProductImage
                {
                    ProductId = product.Id,
                    ImageUrl = $"/uploads/products/{uniqueFileName}",
                    IsPrimary = (i == 0),
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Product updated successfully: {ProductId} ({Title}) by Seller {SellerId}",
            product.Id, product.Title, currentUserId);

        TempData["SuccessMessage"] = "Product updated successfully!";
        return RedirectToAction(nameof(MyProducts));
    }

    // POST: /Products/MarkAsSold/5 (R.3.5, R.13.3)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsSold(int id)
    {
        var currentUserId = _userManager.GetUserId(User)!;
        var product = await _context.Products.FindAsync(id);

        if (product == null) return NotFound();
        if (product.SellerId != currentUserId)
        {
            _logger.LogWarning("Forbidden status change attempt: User {UserId} tried to mark Product {ProductId} owned by {SellerId} as sold",
                currentUserId, id, product.SellerId);
            return Forbid();
        }

        product.Status = ProductStatus.Sold;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product marked as SOLD: {ProductId} ({Title}) by Seller {SellerId}",
            id, product.Title, currentUserId);

        TempData["SuccessMessage"] = "Product marked as SOLD successfully!";
        return RedirectToAction(nameof(MyProducts));
    }

    // POST: /Products/Delete/5 (R.3.4, R.13.3)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = _userManager.GetUserId(User)!;
        var product = await _context.Products.FindAsync(id);

        if (product == null) return NotFound();
        if (product.SellerId != currentUserId)
        {
            _logger.LogWarning("Forbidden delete attempt: User {UserId} tried to delete Product {ProductId} owned by {SellerId}",
                currentUserId, id, product.SellerId);
            return Forbid();
        }

        // If the product has an order, do not delete it
        var hasOrder = await _context.Orders.AnyAsync(o => o.ProductId == id);
        if (hasOrder)
        {
            _logger.LogWarning("Deletion rejected: Product {ProductId} has an existing order record", id);
            TempData["ErrorMessage"] = "This item has a purchase record and cannot be deleted.";
            return RedirectToAction(nameof(MyProducts));
        }

        // Remove images
        var images = await _context.ProductImages.Where(img => img.ProductId == id).ToListAsync();
        _context.ProductImages.RemoveRange(images);

        // Remove wishlist entries
        var wishlists = await _context.Wishlists.Where(w => w.ProductId == id).ToListAsync();
        _context.Wishlists.RemoveRange(wishlists);

        // Remove conversations & messages
        var convs = await _context.ChatConversations.Where(c => c.ProductId == id).ToListAsync();
        var convIds = convs.Select(c => c.Id).ToList();
        var msgs = await _context.ChatMessages.Where(m => convIds.Contains(m.ConversationId)).ToListAsync();
        _context.ChatMessages.RemoveRange(msgs);
        _context.ChatConversations.RemoveRange(convs);

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product deleted: {ProductId} ({Title}) by Seller {SellerId}",
            id, product.Title, currentUserId);

        TempData["SuccessMessage"] = "Product deleted successfully.";
        return RedirectToAction(nameof(MyProducts));
    }
}
