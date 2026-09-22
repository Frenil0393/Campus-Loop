using System.ComponentModel.DataAnnotations;
using CampusLoop.Models;

namespace CampusLoop.ViewModels;

public class CheckoutViewModel
{
    public int ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;

    public string SellerId { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
    public string SellerEmail { get; set; } = string.Empty;
    public string SellerPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a payment method")]
    [Display(Name = "Payment Method")]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.UPI;

    // UPI fields
    [Display(Name = "UPI ID / VPA")]
    public string? UpiId { get; set; }

    // Card fields
    [Display(Name = "Card Number")]
    public string? CardNumber { get; set; }

    [Display(Name = "Valid Thru (MM/YY)")]
    public string? CardExpiry { get; set; }

    [Display(Name = "CVV")]
    public string? CardCvv { get; set; }

    [Required(ErrorMessage = "Please choose a campus pickup location for item handover")]
    [Display(Name = "DDU Campus Handover Spot")]
    public string PickupLocation { get; set; } = "DDU Central Library Foyer";

    [Display(Name = "Handover Note for Seller (Optional)")]
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
    public string TransactionId { get; set; } = string.Empty;
    public DateTime PurchasedAt { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public string PickupLocation { get; set; } = string.Empty;
}
