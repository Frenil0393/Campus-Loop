using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusLoop.Models;

public enum PaymentMethod
{
    UPI = 1,          // Google Pay, PhonePe, Paytm
    Card = 2,         // Debit / Credit Card
    NetBanking = 3,   // Net Banking
    CashOnHandover = 4 // Cash on campus handover
}

public enum OrderStatus
{
    Completed = 1,
    Refunded = 2
}

public class Order : BaseEntity
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    public string BuyerId { get; set; } = string.Empty;

    [Required]
    public string SellerId { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.UPI;

    public OrderStatus Status { get; set; } = OrderStatus.Completed;

    [Required, MaxLength(100)]
    public string TransactionId { get; set; } = string.Empty; // e.g. "TXN-DDU-982341"

    [Required, MaxLength(250)]
    public string PickupLocation { get; set; } = "DDU Central Library"; // Campus Handover Spot

    [MaxLength(500)]
    public string? Notes { get; set; }
}
