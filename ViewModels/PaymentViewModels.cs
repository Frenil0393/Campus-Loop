using System.ComponentModel.DataAnnotations;
using CampusLoop.Models;

namespace CampusLoop.ViewModels;

public class CheckoutViewModel
{
    public int ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string SellerId { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
    public string SellerEmail { get; set; } = string.Empty;
    public string SellerPhone { get; set; } = string.Empty;

    // Payment Selection
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.UpiQr;

    // UPI Fields
    [MaxLength(100)]
    public string? UpiId { get; set; }

    [MaxLength(100)]
    public string? UpiTransactionRef { get; set; }

    // Card Fields (Demo only)
    [MaxLength(19)]
    public string? CardNumber { get; set; }

    [MaxLength(5)]
    public string? CardExpiry { get; set; }

    [MaxLength(4)]
    public string? CardCvv { get; set; }

    [MaxLength(100)]
    public string? CardHolderName { get; set; }

    // Campus Handover Spot
    [Required(ErrorMessage = "Please select a campus meeting point")]
    public string PickupLocation { get; set; } = "DDU Central Library Foyer";

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class OrderReceiptViewModel
{
    public int OrderId { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }
    public DateTime PurchasedAt { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public string SellerPhone { get; set; } = string.Empty;
    public string SellerEmail { get; set; } = string.Empty;
    public string PickupLocation { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class MyPurchaseItemViewModel
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public DateTime PurchasedAt { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public string PickupLocation { get; set; } = string.Empty;
}
