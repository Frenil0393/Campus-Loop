using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusLoop.Data;
using CampusLoop.Models;
using CampusLoop.ViewModels;

namespace CampusLoop.Controllers;

[Authorize] // Requirement: Payment only happens if user is logged in
public class CheckoutController : Controller
{
    private readonly CampusLoopDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CheckoutController(CampusLoopDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /Checkout/Index/5 (Direct Buy Checkout)
    [HttpGet]
    public async Task<IActionResult> Index(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        var currentUserId = _userManager.GetUserId(User)!;

        // Prevent seller buying own item
        if (product.SellerId == currentUserId)
        {
            TempData["ErrorMessage"] = "You cannot purchase your own product listing.";
            return RedirectToAction("Details", "Products", new { id });
        }

        // Prevent buying sold items
        if (product.Status == ProductStatus.Sold)
        {
            TempData["ErrorMessage"] = "This item has already been sold.";
            return RedirectToAction("Details", "Products", new { id });
        }

        var category = await _context.Categories.FindAsync(product.CategoryId);
        var seller = await _userManager.FindByIdAsync(product.SellerId);
        var primaryImage = await _context.ProductImages
            .Where(img => img.ProductId == id)
            .OrderByDescending(img => img.IsPrimary)
            .Select(img => img.ImageUrl)
            .FirstOrDefaultAsync() ?? "/images/placeholder.svg";

        var vm = new CheckoutViewModel
        {
            ProductId = product.Id,
            ProductTitle = product.Title,
            Price = product.Price,
            CategoryName = category?.Name ?? "General",
            ImageUrl = primaryImage,
            SellerId = product.SellerId,
            SellerName = seller?.FullName ?? "DDU Student",
            SellerEmail = seller?.Email ?? "",
            SellerPhone = seller?.PhoneNumber ?? "",
            PaymentMethod = PaymentMethod.UPI,
            PickupLocation = "DDU Central Library Foyer"
        };

        return View(vm);
    }

    // POST: /Checkout/ProcessPayment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessPayment(CheckoutViewModel model)
    {
        var product = await _context.Products.FindAsync(model.ProductId);
        if (product == null) return NotFound();

        var currentUserId = _userManager.GetUserId(User)!;

        if (product.SellerId == currentUserId)
        {
            TempData["ErrorMessage"] = "You cannot purchase your own product listing.";
            return RedirectToAction("Details", "Products", new { id = model.ProductId });
        }

        if (product.Status == ProductStatus.Sold)
        {
            TempData["ErrorMessage"] = "This item was just sold to another student.";
            return RedirectToAction("Index", "Home");
        }

        // Generate verified DDU Campus transaction ID
        var txnId = $"TXN-DDU-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}";

        var order = new Order
        {
            ProductId = product.Id,
            BuyerId = currentUserId,
            SellerId = product.SellerId,
            Amount = product.Price,
            PaymentMethod = model.PaymentMethod,
            Status = OrderStatus.Completed,
            TransactionId = txnId,
            PickupLocation = string.IsNullOrWhiteSpace(model.PickupLocation) ? "DDU Central Library" : model.PickupLocation.Trim(),
            Notes = model.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        // Mark item as SOLD
        product.Status = ProductStatus.Sold;
        product.UpdatedAt = DateTime.UtcNow;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Payment successful! Transaction ID: {txnId}";
        return RedirectToAction(nameof(Receipt), new { id = order.Id });
    }

    // GET: /Checkout/Receipt/10
    [HttpGet]
    public async Task<IActionResult> Receipt(int id)
    {
        var currentUserId = _userManager.GetUserId(User)!;

        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        // Security check: Only buyer or seller or admin can view receipt
        if (order.BuyerId != currentUserId && order.SellerId != currentUserId && !User.IsInRole(Roles.Admin))
        {
            return Forbid();
        }

        var product = await _context.Products.FindAsync(order.ProductId);
        var seller = await _userManager.FindByIdAsync(order.SellerId);
        var primaryImage = await _context.ProductImages
            .Where(img => img.ProductId == order.ProductId)
            .OrderByDescending(img => img.IsPrimary)
            .Select(img => img.ImageUrl)
            .FirstOrDefaultAsync() ?? "/images/placeholder.svg";

        var vm = new OrderReceiptViewModel
        {
            OrderId = order.Id,
            TransactionId = order.TransactionId,
            ProductId = order.ProductId,
            ProductTitle = product?.Title ?? "Product",
            ImageUrl = primaryImage,
            Amount = order.Amount,
            PaymentMethod = order.PaymentMethod,
            PurchasedAt = order.CreatedAt,
            SellerName = seller?.FullName ?? "DDU Student",
            SellerPhone = seller?.PhoneNumber ?? "",
            SellerEmail = seller?.Email ?? "",
            PickupLocation = order.PickupLocation,
            Notes = order.Notes
        };

        return View(vm);
    }

    // GET: /Checkout/MyPurchases
    [HttpGet]
    public async Task<IActionResult> MyPurchases()
    {
        var currentUserId = _userManager.GetUserId(User)!;

        var orders = await _context.Orders
            .Where(o => o.BuyerId == currentUserId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var productIds = orders.Select(o => o.ProductId).Distinct().ToList();
        var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        var images = await _context.ProductImages
            .Where(img => productIds.Contains(img.ProductId) && img.IsPrimary)
            .ToDictionaryAsync(img => img.ProductId, img => img.ImageUrl);

        var sellerIds = orders.Select(o => o.SellerId).Distinct().ToList();
        var sellers = await _context.Users.Where(u => sellerIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName);

        var items = orders.Select(o => new MyPurchaseItemViewModel
        {
            OrderId = o.Id,
            ProductId = o.ProductId,
            ProductTitle = products.TryGetValue(o.ProductId, out var prod) ? prod.Title : "Academic Item",
            ImageUrl = images.TryGetValue(o.ProductId, out var img) ? img : "/images/placeholder.svg",
            Amount = o.Amount,
            TransactionId = o.TransactionId,
            PurchasedAt = o.CreatedAt,
            SellerName = sellers.TryGetValue(o.SellerId, out var sName) ? sName : "DDU Student",
            PickupLocation = o.PickupLocation
        }).ToList();

        return View(items);
    }
}
