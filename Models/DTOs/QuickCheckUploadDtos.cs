using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace MultiVendorEcommerce.Models.DTOs;

/// <summary>
/// Request DTO for creating or updating a QuickCheck with optional image upload.
/// </summary>
public class QuickCheckWithImageRequest
{
    /// <summary>
    /// QuickCheck name (required)
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// QuickCheck description with HTML support (required)
    /// </summary>
    [Required(ErrorMessage = "Description is required")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Position for sorting (default: 0)
    /// </summary>
    public int Position { get; set; } = 0;

    /// <summary>
    /// Whether the QuickCheck is published and visible (default: false)
    /// </summary>
    public bool Published { get; set; } = false;

    /// <summary>
    /// Image file to upload (optional)
    /// </summary>
    public IFormFile? File { get; set; }
}

