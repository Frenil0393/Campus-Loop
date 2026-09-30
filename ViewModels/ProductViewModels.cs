using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using CampusLoop.Models;

namespace CampusLoop.ViewModels;

public class ProductCreateViewModel
{
    [Required(ErrorMessage = "Product title is required")]
    [MaxLength(150)]
    [Display(Name = "Product Title")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a category")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, 1000000.00, ErrorMessage = "Price must be greater than 0")]
    [Display(Name = "Price (₹)")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Description is required")]
    [MaxLength(2000)]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Product Description")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please upload 3 to 4 product images")]
    [Display(Name = "Product Images (3 to 4 required)")]
    public List<IFormFile> ImageFiles { get; set; } = new();
}

public class ProductEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Product title is required")]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a category")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, 1000000.00)]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Description is required")]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public List<string> ExistingImageUrls { get; set; } = new();

    [Display(Name = "Upload New Images (Optional replacement)")]
    public List<IFormFile>? NewImageFiles { get; set; }
}

public class ProductDetailsViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public ProductStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> ImageUrls { get; set; } = new();

    // Seller Information (R.7.1)
    public string SellerId { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
    public string SellerEmail { get; set; } = string.Empty;
    public string SellerPhone { get; set; } = string.Empty;
    public string SellerBranch { get; set; } = string.Empty;
    public int SellerSemester { get; set; }

    public bool IsInWishlist { get; set; }
    public bool IsOwner { get; set; }
}

public class MarketplaceItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string PrimaryImageUrl { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
    public ProductStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsInWishlist { get; set; }
}

public class AdminDashboardViewModel
{
    public int TotalStudents { get; set; }
    public int TotalProducts { get; set; }
    public int AvailableProducts { get; set; }
    public int SoldProducts { get; set; }
    public int TotalCategories { get; set; }
}
