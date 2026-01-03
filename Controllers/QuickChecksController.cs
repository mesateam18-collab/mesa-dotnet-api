using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiVendorEcommerce.Models.DTOs;
using MultiVendorEcommerce.Models.Entities;
using MultiVendorEcommerce.Services;

namespace MultiVendorEcommerce.Controllers;

/// <summary>
/// Controller for managing QuickChecks
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class QuickChecksController(
    IQuickCheckService quickCheckService,
    IImageStorageService imageStorageService) : ControllerBase
{
    private readonly IQuickCheckService _quickCheckService = quickCheckService;
    private readonly IImageStorageService _imageStorageService = imageStorageService;

    /// <summary>
    /// Get all QuickChecks (public endpoint)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<QuickCheck>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<QuickCheck>>> GetAll()
    {
        var quickChecks = await _quickCheckService.GetAllAsync();
        return Ok(quickChecks);
    }

    /// <summary>
    /// Get a QuickCheck by ID (public endpoint)
    /// </summary>
    /// <param name="id">QuickCheck ID</param>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(QuickCheck), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuickCheck>> GetById(string id)
    {
        var quickCheck = await _quickCheckService.GetByIdAsync(id);
        if (quickCheck is null)
        {
            return NotFound();
        }
        return Ok(quickCheck);
    }

    /// <summary>
    /// Create a new QuickCheck with optional image (Admin only)
    /// </summary>
    /// <param name="request">QuickCheck data and optional image file</param>
    /// <returns>The created QuickCheck</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(10_000_000)] // 10MB limit
    [ProducesResponseType(typeof(QuickCheck), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<QuickCheck>> Create([FromForm] QuickCheckWithImageRequest request)
    {
        // Validate model
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Create QuickCheck entity
        var quickCheck = new QuickCheck
        {
            Id = string.Empty,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Position = request.Position,
            Published = request.Published,
            ImageUrl = null,
            CreatedAt = DateTime.UtcNow
        };

        // Upload image if provided
        if (request.File != null && request.File.Length > 0)
        {
            try
            {
                await using var stream = request.File.OpenReadStream();
                var url = await _imageStorageService.UploadAsync(
                    stream,
                    request.File.FileName,
                    request.File.ContentType,
                    HttpContext.RequestAborted,
                    "quickchecks");

                quickCheck.ImageUrl = url;
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to upload image: {ex.Message}");
            }
        }

        // Save to database
        try
        {
            var created = await _quickCheckService.CreateAsync(quickCheck);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while creating the QuickCheck: {ex.Message}");
        }
    }

    /// <summary>
    /// Update an existing QuickCheck with optional new image (Admin only)
    /// </summary>
    /// <param name="id">QuickCheck ID</param>
    /// <param name="request">Updated QuickCheck data and optional new image file</param>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(10_000_000)] // 10MB limit
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromForm] QuickCheckWithImageRequest request)
    {
        // Validate model
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if QuickCheck exists
        var existing = await _quickCheckService.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        // Update QuickCheck entity
        var quickCheck = new QuickCheck
        {
            Id = id,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Position = request.Position,
            Published = request.Published,
            ImageUrl = existing.ImageUrl, // Preserve existing image
            CreatedAt = existing.CreatedAt, // Preserve creation date
            UpdatedAt = DateTime.UtcNow
        };

        // Handle new image if provided
        if (request.File != null && request.File.Length > 0)
        {
            try
            {
                await using var stream = request.File.OpenReadStream();
                var url = await _imageStorageService.UploadAsync(
                    stream,
                    request.File.FileName,
                    request.File.ContentType,
                    HttpContext.RequestAborted,
                    "quickchecks");

                quickCheck.ImageUrl = url; // Replace with new image
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to upload image: {ex.Message}");
            }
        }

        // Update in database
        try
        {
            var updated = await _quickCheckService.UpdateAsync(id, quickCheck);
            if (!updated)
            {
                return NotFound();
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while updating the QuickCheck: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a QuickCheck (Admin only)
    /// </summary>
    /// <param name="id">QuickCheck ID</param>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _quickCheckService.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }

    /// <summary>
    /// Upload a content image for use in QuickCheck description (Admin only)
    /// </summary>
    /// <param name="file">Image file to upload</param>
    /// <returns>The uploaded image URL</returns>
    [HttpPost("upload-image")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(10_000_000)] // 10MB limit
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<string>> UploadContentImage(IFormFile file)
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
                "quickchecks");

            return Ok(new { url = url });
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to upload image: {ex.Message}");
        }
    }
}

