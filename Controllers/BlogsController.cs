using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiVendorEcommerce.Models.DTOs;
using MultiVendorEcommerce.Models.Entities;
using MultiVendorEcommerce.Services;

namespace MultiVendorEcommerce.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BlogsController(IBlogService blogService, IImageStorageService imageStorageService) : ControllerBase
{
    private readonly IBlogService _blogService = blogService;
    private readonly IImageStorageService _imageStorageService = imageStorageService;

    // Public list
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Blog>>> GetAll()
    {
        var blogs = await _blogService.GetAllAsync();
        return Ok(blogs);
    }

    // Public get by id
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Blog>> GetById(string id)
    {
        var blog = await _blogService.GetByIdAsync(id);
        if (blog is null)
        {
            return NotFound();
        }

        return Ok(blog);
    }

    // Admin: create blog with optional images
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<Blog>> Create([FromForm] BlogWithImagesRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BlogJson))
        {
            return BadRequest("Blog payload is required.");
        }

        Blog? blog;
        try
        {
            blog = JsonSerializer.Deserialize<Blog>(request.BlogJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return BadRequest("Invalid blog JSON.");
        }

        if (blog is null)
        {
            return BadRequest("Blog payload is invalid.");
        }

        // Handle images if provided: first file -> ImageUrl, rest -> ContentImages
        if (request.Files is { Count: > 0 })
        {
            for (var i = 0; i < request.Files.Count; i++)
            {
                var file = request.Files[i];
                if (file.Length <= 0)
                {
                    continue;
                }

                await using var stream = file.OpenReadStream();
                var url = await _imageStorageService.UploadAsync(
                    stream,
                    file.FileName,
                    file.ContentType,
                    HttpContext.RequestAborted);

                if (i == 0)
                {
                    blog.ImageUrl = url;
                }
                else
                {
                    blog.ContentImages.Add(url);
                }
            }
        }

        var created = await _blogService.CreateAsync(blog);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // Admin: update blog with optional new images
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Update(string id, [FromForm] BlogWithImagesRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BlogJson))
        {
            return BadRequest("Blog payload is required.");
        }

        var existing = await _blogService.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        Blog? blog;
        try
        {
            blog = JsonSerializer.Deserialize<Blog>(request.BlogJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return BadRequest("Invalid blog JSON.");
        }

        if (blog is null)
        {
            return BadRequest("Blog payload is invalid.");
        }

        // Start with existing images
        blog.ImageUrl = existing.ImageUrl;
        blog.ContentImages = existing.ContentImages ?? new List<string>();

        // If new files are provided, overwrite ImageUrl with first and append rest to ContentImages
        if (request.Files is { Count: > 0 })
        {
            blog.ContentImages = blog.ContentImages ?? new List<string>();

            for (var i = 0; i < request.Files.Count; i++)
            {
                var file = request.Files[i];
                if (file.Length <= 0)
                {
                    continue;
                }

                await using var stream = file.OpenReadStream();
                var url = await _imageStorageService.UploadAsync(
                    stream,
                    file.FileName,
                    file.ContentType,
                    HttpContext.RequestAborted);

                if (i == 0)
                {
                    blog.ImageUrl = url;
                }
                else
                {
                    blog.ContentImages.Add(url);
                }
            }
        }

        var ok = await _blogService.UpdateAsync(id, blog);
        if (!ok)
        {
            return NotFound();
        }

        return NoContent();
    }

    // Admin: delete blog
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        var ok = await _blogService.DeleteAsync(id);
        if (!ok)
        {
            return NotFound();
        }

        return NoContent();
    }
}
