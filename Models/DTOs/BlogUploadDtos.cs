using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace MultiVendorEcommerce.Models.DTOs;

/// <summary>
/// Request DTO for creating or updating a blog post with optional image uploads.
/// </summary>
public class BlogWithImagesRequest
{
    /// <summary>
    /// Blog post title (required, max 200 characters)
    /// </summary>
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Blog post content/body text (required)
    /// </summary>
    [Required(ErrorMessage = "Body content is required")]
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Whether the blog post is published and visible to the public (default: false)
    /// </summary>
    public bool Published { get; set; } = false;

    /// <summary>
    /// Image files to upload. 
    /// - The first file becomes the cover image (ImageUrl)
    /// - Additional files are added to ContentImages array
    /// </summary>
    public List<IFormFile> Files { get; set; } = new();
}
