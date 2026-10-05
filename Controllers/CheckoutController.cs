using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusLoop.Data;
using CampusLoop.Models;
using CampusLoop.ViewModels;

namespace CampusLoop.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly CampusLoopDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<CheckoutController> _logger;

    public CheckoutController(
        CampusLoopDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<CheckoutController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    // GET: /Checkout/Index/5
    [HttpGet]
    public async Task<IActionResult> Index(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        var currentUserId = _userManager.GetUserId(User)!;
        _logger.LogInformation("User {UserId} initiated checkout for Product {ProductId} ({Title})",
            currentUserId, product.Id, product.Title);

        // Prevent seller buying own item
        if (product.SellerId == currentUserId)
        {
            _logger.LogWarning("Checkout blocked: User {UserId} tried to purchase their own product {ProductId}", currentUserId, id);
            TempData["ErrorMessage"] = "You cannot buy your own product listing.";
            return RedirectToAction("Details", "Products", new { id });
        }

        // Prevent buying sold items
        if (product.Status == ProductStatus.Sold)
        {
            _logger.LogWarning("Checkout blocked: Product {ProductId} is already marked as sold", id);
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
            PaymentMethod = PaymentMethod.UpiQr,
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
            _logger.LogWarning("Payment rejected: User {UserId} cannot buy their own product {ProductId}", currentUserId, model.ProductId);
            TempData["ErrorMessage"] = "You cannot buy your own product listing.";
            return RedirectToAction("Details", "Products", new { id = model.ProductId });
        }

        if (product.Status == ProductStatus.Sold)
        {
            _logger.LogWarning("Payment rejected: Product {ProductId} already sold when User {UserId} submitted checkout", model.ProductId, currentUserId);
            TempData["ErrorMessage"] = "This item has already been marked as sold.";
            return RedirectToAction("Index", "Home");
        }

        // ----- Check required payment fields -----
        ValidatePaymentDetails(model);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Payment validation failed for Product {ProductId}, Method: {PaymentMethod}, Buyer: {BuyerId}",
                model.ProductId, model.PaymentMethod, currentUserId);
            await FillCheckoutDisplayDataAsync(model, product);
            return View("Index", model);
        }

        // Generate college-verified transaction ID
        var txnId = $"TXN-DDU-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}".ToUpperInvariant();

        // Determine payment reference for display
        string? reference = model.PaymentMethod switch
        {
            PaymentMethod.UpiQr => model.UpiTransactionRef!.Trim(),
            PaymentMethod.UpiId => model.UpiId!.Trim(),
            PaymentMethod.Card => GetCardReference(model.CardNumber),
            _ => "Cash on Handover"
        };

        var order = new Order
        {
            ProductId = product.Id,
            BuyerId = currentUserId,
            SellerId = product.SellerId,
            Amount = product.Price,
            PaymentMethod = model.PaymentMethod,
            TransactionId = txnId,
            PaymentReference = reference,
            PickupLocation = string.IsNullOrWhiteSpace(model.PickupLocation) ? "DDU Central Library Foyer" : model.PickupLocation.Trim(),
            Notes = model.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;
            var rowsUpdated = await _context.Products
                .Where(p => p.Id == model.ProductId && p.Status == ProductStatus.Available)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.Status, ProductStatus.Sold)
                    .SetProperty(p => p.UpdatedAt, now));

            if (rowsUpdated == 0)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning("Concurrent purchase detected: Product {ProductId} was already claimed. Transaction rolled back for User {UserId}",
                    model.ProductId, currentUserId);
                TempData["ErrorMessage"] = "This item has already been sold.";
                return RedirectToAction("Details", "Products", new { id = model.ProductId });
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var buyer = await _userManager.FindByIdAsync(currentUserId);
            var buyerName = buyer?.FullName ?? "A student";
            var receiptUrl = Url.Action(nameof(Receipt), "Checkout", new { id = order.Id }) ?? $"/Checkout/Receipt/{order.Id}";
            var notification = new Notification
            {
                UserId = product.SellerId,
                Message = $"{buyerName} bought your item {product.Title}",
                LinkUrl = receiptUrl,
                IsRead = false,
                ConversationId = null,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation("Order created successfully! OrderId: {OrderId}, TxnId: {TxnId}, Amount: {Amount}, Method: {PaymentMethod}, Buyer: {BuyerId}, Seller: {SellerId}",
                order.Id, txnId, order.Amount, order.PaymentMethod, currentUserId, product.SellerId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Transaction failed while processing order for Product {ProductId} by Buyer {BuyerId}",
                model.ProductId, currentUserId);
            throw;
        }

        TempData["SuccessMessage"] = $"Payment successful! Transaction ID: {txnId}";
        return RedirectToAction(nameof(Receipt), new { id = order.Id });
    }

    // GET: /Checkout/Receipt/5
    [HttpGet]
    public async Task<IActionResult> Receipt(int id)
    {
        var currentUserId = _userManager.GetUserId(User)!;

        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        // Security check: Only buyer or seller or admin can view
        if (order.BuyerId != currentUserId && order.SellerId != currentUserId && !User.IsInRole(Roles.Admin))
        {
            _logger.LogWarning("Unauthorized receipt view attempt for Order {OrderId} by User {UserId}", id, currentUserId);
            return Forbid();
        }

        _logger.LogInformation("Receipt viewed for Order {OrderId} by User {UserId}", id, currentUserId);

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
            ProductTitle = product?.Title ?? "Academic Item",
            ImageUrl = primaryImage,
            Amount = order.Amount,
            PaymentMethod = order.PaymentMethod,
            PaymentReference = order.PaymentReference,
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
            PaymentMethod = o.PaymentMethod,
            TransactionId = o.TransactionId,
            PurchasedAt = o.CreatedAt,
            SellerName = sellers.TryGetValue(o.SellerId, out var sName) ? sName : "DDU Student",
            PickupLocation = o.PickupLocation
        }).ToList();

        return View(items);
    }

    // ---------- Payment field checks (required fields only) ----------
    private void ValidatePaymentDetails(CheckoutViewModel model)
    {
        switch (model.PaymentMethod)
        {
            case PaymentMethod.UpiQr:
                if (string.IsNullOrWhiteSpace(model.UpiTransactionRef))
                    ModelState.AddModelError(nameof(model.UpiTransactionRef),
                        "Please enter the UPI transaction reference.");
                break;

            case PaymentMethod.UpiId:
                if (string.IsNullOrWhiteSpace(model.UpiId))
                    ModelState.AddModelError(nameof(model.UpiId),
                        "Please enter your UPI ID.");
                break;

            case PaymentMethod.Card:
                if (string.IsNullOrWhiteSpace(model.CardHolderName))
                    ModelState.AddModelError(nameof(model.CardHolderName), "Please enter the card holder name.");

                if (string.IsNullOrWhiteSpace(model.CardNumber))
                    ModelState.AddModelError(nameof(model.CardNumber), "Please enter the card number.");

                if (string.IsNullOrWhiteSpace(model.CardExpiry))
                    ModelState.AddModelError(nameof(model.CardExpiry), "Please enter the card expiry date.");

                if (string.IsNullOrWhiteSpace(model.CardCvv))
                    ModelState.AddModelError(nameof(model.CardCvv), "Please enter the CVV.");
                break;

                // Cash: nothing to check
        }
    }

    private static string GetCardReference(string? cardNumber)
    {
        var digits = new string((cardNumber ?? "").Where(char.IsDigit).ToArray());
        return digits.Length >= 4 ? $"Card ending in {digits[^4..]}" : "Card Payment";
    }

    // The posted form does not contain product/seller details, so reload them
    private async Task FillCheckoutDisplayDataAsync(CheckoutViewModel model, Product product)
    {
        var category = await _context.Categories.FindAsync(product.CategoryId);
        var seller = await _userManager.FindByIdAsync(product.SellerId);
        var primaryImage = await _context.ProductImages
            .Where(img => img.ProductId == product.Id)
            .OrderByDescending(img => img.IsPrimary)
            .Select(img => img.ImageUrl)
            .FirstOrDefaultAsync() ?? "/images/placeholder.svg";

        model.ProductTitle = product.Title;
        model.Price = product.Price;
        model.CategoryName = category?.Name ?? "General";
        model.ImageUrl = primaryImage;
        model.SellerId = product.SellerId;
        model.SellerName = seller?.FullName ?? "DDU Student";
        model.SellerEmail = seller?.Email ?? "";
        model.SellerPhone = seller?.PhoneNumber ?? "";
    }
}