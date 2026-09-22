using System.ComponentModel.DataAnnotations;

namespace CampusLoop.Models;

public enum PaymentMethodType
{
    UpiQr,
    Card,
    NetBanking,
    CashOnHandover
}

public enum PaymentStatus
{
    Pending,
    Completed,
    Failed
}

public class PaymentOrder
{
    [Key]
    public string OrderId { get; set; } = string.Empty;

    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string BuyerId { get; set; } = string.Empty;
    public string BuyerName { get; set; } = "Student Buyer";
    public string BuyerEmail { get; set; } = "24ceuos155@ddu.ac.in";
    public string SellerName { get; set; } = string.Empty;
    public string SellerUpiId { get; set; } = string.Empty;
    public string CampusMeetingSpot { get; set; } = "DDU Central Library / Canteen";
    public PaymentMethodType Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string TransactionReference { get; set; } = string.Empty;
    public string HandshakeOtp { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PaymentRequest
{
    public int ProductId { get; set; }
    public string PaymentMethod { get; set; } = "UpiQr"; // "UpiQr", "Card", "NetBanking", "CashOnHandover"
    public decimal Amount { get; set; }
    public string? UpiTransactionId { get; set; }
    public string? CardNumber { get; set; }
    public string? BankName { get; set; }
    public string? BuyerNote { get; set; }
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string HandshakeOtp { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
    public string MeetingLocation { get; set; } = string.Empty;
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
}
