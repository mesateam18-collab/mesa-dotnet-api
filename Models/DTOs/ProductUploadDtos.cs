using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace MultiVendorEcommerce.Models.DTOs;

/// <summary>
/// Request DTO for creating/updating a product with optional images.
/// Uses typed form fields (multipart/form-data), no JSON blob.
/// </summary>
public class CreateProductWithImagesRequest
{
    /// <summary>
    /// Product name (required, max 200 chars)
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Product description (optional)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Base price (required)
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Price must be >= 0")]
    public decimal Price { get; set; }

    /// <summary>
    /// Sales price (optional, defaults to 0)
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Sales price must be >= 0")]
    public decimal SalesPrice { get; set; } = 0;

    /// <summary>
    /// Stock quantity (required)
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be >= 0")]
    public int StockQuantity { get; set; }

    /// <summary>
    /// Stock status (true = in stock)
    /// </summary>
    public bool StockStatus { get; set; } = true;

    /// <summary>
    /// Vendor ID (required for Admin; ignored for Vendor role because it is resolved from the current user)
    /// </summary>
    public string? VendorId { get; set; }

    /// <summary>
    /// Categories as a comma-separated list (e.g., "shoes, men, sneakers")
    /// </summary>
    public string? CategoriesCsv { get; set; }

    /// <summary>
    /// Optional attributes provided as key/value pairs
    /// </summary>
    public Dictionary<string, string>? Attributes { get; set; }

    /// <summary>
    /// Image files to upload. All images are added to ImageUrls; first can be treated as primary by the UI.
    /// </summary>
    public List<IFormFile> Files { get; set; } = new();
}
