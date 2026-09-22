using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusLoop.Data;
using CampusLoop.Models;

namespace CampusLoop.Controllers;

public class PaymentController : Controller
{
    private readonly CampusLoopDbContext _context;

    public PaymentController(CampusLoopDbContext context)
    {
        _context = context;
    }

    // GET: /Payment/Checkout?productId=1
    // Full standalone HTML Checkout page (pure HTML MVC)
    [HttpGet]
    public async Task<IActionResult> Checkout(int productId)
    {
        var product = await _context.Products
            .Include(p => p.Seller)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
        {
            TempData["Error"] = "Item not found or has already been removed.";
            return RedirectToAction("Index", "Home");
        }

        if (product.Status == "SOLD")
        {
            TempData["Info"] = "This item has already been marked as SOLD.";
            return RedirectToAction("Details", "Product", new { id = productId });
        }

        return View(product);
    }

    // POST: /Payment/ProcessCheckout
    // Standard HTML form submission (works seamlessly with pure HTML)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessCheckout(int productId, string paymentMethod, string? buyerNotes)
    {
        var product = await _context.Products
            .Include(p => p.Seller)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null || product.Status == "SOLD")
        {
            TempData["Error"] = "Product is no longer available.";
            return RedirectToAction("Index", "Home");
        }

        var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "guest_buyer";
        var buyerName = User.Identity?.Name ?? "Student Buyer";
        var buyerEmail = User.FindFirstValue(ClaimTypes.Email) ?? "24ceuos155@ddu.ac.in";

        string orderId = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        string txnId = $"TXN-LOOP-{Random.Shared.Next(100000, 999999)}";
        string otp = Random.Shared.Next(1000, 9999).ToString();

        var methodType = paymentMethod switch
        {
            "Card" => PaymentMethodType.Card,
            "NetBanking" => PaymentMethodType.NetBanking,
            "CashOnHandover" => PaymentMethodType.CashOnHandover,
            _ => PaymentMethodType.UpiQr
        };

        var order = new PaymentOrder
        {
            OrderId = orderId,
            ProductId = product.Id,
            ProductName = product.Name,
            Amount = product.Price,
            BuyerId = buyerId,
            BuyerName = buyerName,
            BuyerEmail = buyerEmail,
            SellerName = product.Seller?.FullName ?? "Campus Seller",
            SellerUpiId = $"{product.Seller?.FullName.Replace(" ", "").ToLower()}@okddu",
            CampusMeetingSpot = product.CampusLocation,
            Method = methodType,
            Status = PaymentStatus.Completed,
            TransactionReference = txnId,
            HandshakeOtp = otp,
            CreatedAt = DateTime.UtcNow
        };

        _context.PaymentOrders.Add(order);
        product.Status = "SOLD";
        await _context.SaveChangesAsync();

        TempData["Success"] = "Payment & Handshake booking confirmed!";
        return RedirectToAction("Success", new { orderId = order.OrderId });
    }

    // GET: /Payment/Success?orderId=ORD-...
    // HTML Confirmation & Physical Handshake OTP receipt
    [HttpGet]
    public async Task<IActionResult> Success(string orderId)
    {
        if (string.IsNullOrEmpty(orderId))
        {
            return RedirectToAction("Index", "Home");
        }

        var order = await _context.PaymentOrders
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
        {
            TempData["Error"] = "Order record not found.";
            return RedirectToAction("Index", "Home");
        }

        return View(order);
    }

    // GET: /Payment/Orders
    // Student Orders list showing all transactions and OTPs
    [HttpGet]
    public async Task<IActionResult> Orders()
    {
        var orders = await _context.PaymentOrders
            .OrderByDescending(o => o.CreatedAt)
            .Take(50)
            .ToListAsync();

        return View(orders);
    }

    // POST: /Payment/VerifyOtp
    // Handshake OTP verification during physical item exchange
    [HttpPost]
    public async Task<IActionResult> VerifyOtp(string orderId, string otp)
    {
        var order = await _context.PaymentOrders.FirstOrDefaultAsync(o => o.OrderId == orderId);
        if (order == null)
        {
            return Json(new { success = false, message = "Order not found." });
        }

        if (order.HandshakeOtp == otp?.Trim())
        {
            return Json(new { success = true, message = "Handshake OTP verified successfully! Deal finalized." });
        }

        return Json(new { success = false, message = "Incorrect OTP. Please check the 4-digit code in buyer receipt." });
    }

    // GET: /Payment/GetPaymentDetails?productId=1
    [HttpGet]
    public async Task<IActionResult> GetPaymentDetails(int productId)
    {
        var product = await _context.Products
            .Include(p => p.Seller)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
        {
            return NotFound(new { success = false, message = "Product not found" });
        }

        var seller = product.Seller;
        string sanitizedSellerName = seller?.FullName.Replace(" ", "").ToLower() ?? "seller";
        string sellerUpiId = $"{sanitizedSellerName}@okddu";

        return Json(new
        {
            success = true,
            productId = product.Id,
            productName = product.Name,
            price = product.Price,
            image = product.ImageUrls.FirstOrDefault() ?? "",
            condition = product.Condition,
            location = product.CampusLocation,
            seller = new
            {
                name = seller?.FullName,
                branch = seller?.Branch,
                semester = seller?.Semester,
                phone = seller?.PhoneNumber,
                email = seller?.CollegeEmail,
                upiId = sellerUpiId
            }
        });
    }

    // POST: /Payment/ProcessPayment
    [HttpPost]
    public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
    {
        if (request == null || request.ProductId <= 0)
        {
            return BadRequest(new { success = false, message = "Invalid payment request" });
        }

        var product = await _context.Products
            .Include(p => p.Seller)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId);

        if (product == null)
        {
            return NotFound(new { success = false, message = "Product does not exist" });
        }

        var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "buyer_guest";
        var buyerName = User.Identity?.Name ?? "Student Buyer";

        // Generate Transaction ID and Security OTP
        string orderId = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        string txnId = $"TXN-LOOP-{Random.Shared.Next(100000, 999999)}";
        string otp = Random.Shared.Next(1000, 9999).ToString();

        var order = new PaymentOrder
        {
            OrderId = orderId,
            ProductId = product.Id,
            ProductName = product.Name,
            Amount = request.Amount > 0 ? request.Amount : product.Price,
            BuyerId = buyerId,
            BuyerName = buyerName,
            SellerName = product.Seller?.FullName ?? "Campus Seller",
            SellerUpiId = $"{product.Seller?.FullName.Replace(" ", "").ToLower()}@okddu",
            CampusMeetingSpot = product.CampusLocation,
            Method = request.PaymentMethod switch
            {
                "Card" => PaymentMethodType.Card,
                "NetBanking" => PaymentMethodType.NetBanking,
                "CashOnHandover" => PaymentMethodType.CashOnHandover,
                _ => PaymentMethodType.UpiQr
            },
            Status = PaymentStatus.Completed,
            TransactionReference = txnId,
            HandshakeOtp = otp,
            CreatedAt = DateTime.UtcNow
        };

        _context.PaymentOrders.Add(order);

        // Update product status to SOLD if payment is complete
        product.Status = "SOLD";
        await _context.SaveChangesAsync();

        return Json(new PaymentResult
        {
            Success = true,
            Message = request.PaymentMethod == "CashOnHandover" 
                ? "Cash on Handover booking confirmed! Show your exchange OTP at meeting." 
                : "Payment successful! Your order has been placed.",
            OrderId = order.OrderId,
            TransactionId = order.TransactionReference,
            HandshakeOtp = order.HandshakeOtp,
            Amount = order.Amount,
            PaymentMethod = request.PaymentMethod,
            ProductName = order.ProductName,
            SellerName = order.SellerName,
            MeetingLocation = order.CampusMeetingSpot,
            PaidAt = order.CreatedAt
        });
    }
}
