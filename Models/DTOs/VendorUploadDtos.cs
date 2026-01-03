using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace MultiVendorEcommerce.Models.DTOs;

/// <summary>
/// Request DTO for creating/updating a vendor with optional banner upload.
/// Uses typed form fields (multipart/form-data), no JSON blob.
/// </summary>
public class VendorWithBannerRequest
{
    /// <summary>
    /// User ID that owns this vendor (optional - will be created automatically if Email and Password are provided)
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Email for vendor account creation (required when creating new vendor, optional for updates)
    /// </summary>
    [EmailAddress(ErrorMessage = "Email is invalid")]
    public string? VendorEmail { get; set; }

    /// <summary>
    /// Password for vendor account creation (required when creating new vendor, not used for updates)
    /// </summary>
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
    public string? VendorPassword { get; set; }

    /// <summary>
    /// Business name (required, max 200 chars)
    /// </summary>
    [Required(ErrorMessage = "BusinessName is required")]
    [StringLength(200, ErrorMessage = "BusinessName cannot exceed 200 characters")]
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>
    /// Description (optional)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Notice (optional)
    /// </summary>
    public string? Notice { get; set; }

    /// <summary>
    /// Status (optional)
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Phone (optional)
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Email (optional)
    /// </summary>
    [EmailAddress(ErrorMessage = "Email is invalid")]
    public string? Email { get; set; }

    /// <summary>
    /// Location (optional)
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Commission rate (optional, defaults to 0)
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "CommissionRate must be >= 0")]
    public decimal CommissionRate { get; set; }

    /// <summary>
    /// Rating (optional)
    /// </summary>
    [Range(0, 5, ErrorMessage = "Rating must be between 0 and 5")]
    public decimal? Rating { get; set; }

    /// <summary>
    /// Whether the vendor is approved (optional)
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// Optional banner image to upload
    /// </summary>
    public IFormFile? Banner { get; set; }
}
