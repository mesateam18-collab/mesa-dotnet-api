using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiVendorEcommerce.Models.DTOs;
using MultiVendorEcommerce.Models.Entities;
using MultiVendorEcommerce.Services;

namespace MultiVendorEcommerce.Controllers;

/// <summary>
/// Controller for managing blog posts
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BlogsController : ControllerBase
{
    private readonly IBlogService _blogService;
    private readonly IImageStorageService _imageStorageService;

    public BlogsController(IBlogService blogService, IImageStorageService imageStorageService)
    {
        _blogService = blogService;
        _imageStorageService = imageStorageService;
    }

    /// <summary>
    /// Get all blog posts (public endpoint)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<Blog>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Blog>>> GetAll()
    {
        var blogs = await _blogService.GetAllAsync();
        Console.WriteLine("Blogs: " + blogs);
        Console.WriteLine("Blogs: " + blogs.Count());
        Console.WriteLine("Blogs: " + blogs.First().Title);
        Console.WriteLine("Blogs: " + blogs.First().Body);
        Console.WriteLine("Blogs: " + blogs.First().ImageUrl);
        Console.WriteLine("Blogs: " + blogs.First().ContentImages);
        Console.WriteLine("Blogs: " + blogs.First().Published);
        Console.WriteLine("Blogs: " + blogs.First().CreatedAt);
        Console.WriteLine("Blogs: " + blogs.First().UpdatedAt);
        return Ok(blogs);
    }

    /// <summary>
    /// Get a blog post by ID (public endpoint)
    /// </summary>
    /// <param name="id">Blog post ID</param>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Blog), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Blog>> GetById(string id)
    {
        var blog = await _blogService.GetByIdAsync(id);
        if (blog is null)
        {
            return NotFound();
        }
        return Ok(blog);
    }

    /// <summary>
    /// Create a new blog post with optional images (Admin only)
    /// </summary>
    /// <param name="request">Blog data and optional image files</param>
    /// <returns>The created blog post</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(10_000_000)] // 10MB limit
    [ProducesResponseType(typeof(Blog), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Blog>> Create([FromForm] BlogWithImagesRequest request)
    {
        // Validate model
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Create blog entity
        var blog = new Blog
        {
            Id = string.Empty,
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            Published = request.Published,
            ImageUrl = null,
            ContentImages = new List<string>(),
            CreatedAt = DateTime.UtcNow
        };

        // Upload images if provided
        if (request.Files != null && request.Files.Count > 0)
        {
            var imageUploadResult = await UploadImagesAsync(request.Files);
            if (imageUploadResult.IsError)
            {
                return BadRequest(imageUploadResult.ErrorMessage);
            }

            blog.ImageUrl = imageUploadResult.CoverImageUrl;
            blog.ContentImages = imageUploadResult.ContentImageUrls;
        }

        // Save to database
        try
        {
            var created = await _blogService.CreateAsync(blog);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while creating the blog: {ex.Message}");
        }
    }

    /// <summary>
    /// Update an existing blog post with optional new images (Admin only)
    /// </summary>
    /// <param name="id">Blog post ID</param>
    /// <param name="request">Updated blog data and optional new image files</param>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(10_000_000)] // 10MB limit
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromForm] BlogWithImagesRequest request)
    {
        // Validate model
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if blog exists
        var existing = await _blogService.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        // Update blog entity
        var blog = new Blog
        {
            Id = id,
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            Published = request.Published,
            ImageUrl = existing.ImageUrl, // Preserve existing cover image
            ContentImages = existing.ContentImages ?? new List<string>(), // Preserve existing content images
            CreatedAt = existing.CreatedAt, // Preserve creation date
            UpdatedAt = DateTime.UtcNow
        };

        // Handle new images if provided
        if (request.Files != null && request.Files.Count > 0)
        {
            var imageUploadResult = await UploadImagesAsync(request.Files);
            if (imageUploadResult.IsError)
            {
                return BadRequest(imageUploadResult.ErrorMessage);
            }

            // Replace cover image if first file was uploaded
            if (imageUploadResult.CoverImageUrl != null)
            {
                blog.ImageUrl = imageUploadResult.CoverImageUrl;
            }

            // Add new content images to existing ones
            if (imageUploadResult.ContentImageUrls != null && imageUploadResult.ContentImageUrls.Count > 0)
            {
                blog.ContentImages.AddRange(imageUploadResult.ContentImageUrls);
            }
        }

        // Update in database
        try
        {
            var updated = await _blogService.UpdateAsync(id, blog);
            if (!updated)
            {
                return NotFound();
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while updating the blog: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a blog post (Admin only)
    /// </summary>
    /// <param name="id">Blog post ID</param>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _blogService.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }

    /// <summary>
    /// Upload a content image for use in blog content (Admin only)
    /// </summary>
    /// <param name="file">Image file to upload</param>
    /// <returns>The uploaded image URL</returns>
    [HttpPost("upload-image")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(10_000_000)] // 10MB limit
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> UploadContentImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file provided");
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var url = await _imageStorageService.UploadAsync(
                stream,
                file.FileName,
                file.ContentType,
                HttpContext.RequestAborted,
                "blogs/content"); // Store in a subfolder for content images

            return Ok(new { url = url });
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to upload image: {ex.Message}");
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Uploads image files and returns the cover image URL and content image URLs
    /// </summary>
    private async Task<(string? CoverImageUrl, List<string> ContentImageUrls, bool IsError, string? ErrorMessage)> UploadImagesAsync(
        List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
        {
            return (null, new List<string>(), false, null);
        }

        string? coverImageUrl = null;
        var contentImageUrls = new List<string>();

        for (var i = 0; i < files.Count; i++)
        {
            var file = files[i];
            if (file.Length <= 0)
            {
                continue;
            }

            try
            {
                await using var stream = file.OpenReadStream();
                var url = await _imageStorageService.UploadAsync(
                    stream,
                    file.FileName,
                    file.ContentType,
                    HttpContext.RequestAborted,
                    "blogs");

                // URL is already prefixed with PublicBaseUrl from ImageStorageService
                // This URL will be stored in the database and returned to the frontend
                if (i == 0)
                {
                    coverImageUrl = url;
                    Console.WriteLine($"📸 Cover image URL (stored in database): {url}");
                }
                else
                {
                    contentImageUrls.Add(url);
                    Console.WriteLine($"📸 Content image URL (stored in database): {url}");
                }
            }
            catch (Exception ex)
            {
                return (null, new List<string>(), true, $"Failed to upload image '{file.FileName}': {ex.Message}");
            }
        }

        return (coverImageUrl, contentImageUrls, false, null);
    }

    #endregion
}
