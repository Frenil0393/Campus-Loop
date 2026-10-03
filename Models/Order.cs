using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusLoop.Models;

public enum PaymentMethod
{
    [Display(Name = "UPI QR Code")]
    UpiQr = 1,

    [Display(Name = "UPI ID")]
    UpiId = 2,

    [Display(Name = "Debit / Credit Card")]
    Card = 3,

    [Display(Name = "Cash on Campus Handover")]
    Cash = 4
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

    [Required]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.UpiQr;

    [Required, MaxLength(100)]
    public string TransactionId { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? PaymentReference { get; set; }

    [Required, MaxLength(250)]
    public string PickupLocation { get; set; } = "DDU Central Library";

    [MaxLength(500)]
    public string? Notes { get; set; }

    public Product? Product { get; set; }

    public ApplicationUser? Buyer { get; set; }

    public ApplicationUser? Seller { get; set; }
}
