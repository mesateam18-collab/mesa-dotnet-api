using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiVendorEcommerce.Models.DTOs;
using MultiVendorEcommerce.Models.Entities;
using MultiVendorEcommerce.Services;

namespace MultiVendorEcommerce.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VendorsController(IVendorService vendorService, IImageStorageService imageStorageService) : ControllerBase
{
    private readonly IVendorService _vendorService = vendorService;
    private readonly IImageStorageService _imageStorageService = imageStorageService;

    // Admin: list all vendors
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<Vendor>>> GetAll()
    {
        var vendors = await _vendorService.GetAllAsync();
        return Ok(vendors);
    }

    // Admin: get vendor by id
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Vendor>> GetById(string id)
    {
        var vendor = await _vendorService.GetByIdAsync(id);
        if (vendor is null)
        {
            return NotFound();
        }

        return Ok(vendor);
    }

    // Vendor/Admin: get current user's vendor profile
    [HttpGet("me")]
    [Authorize(Roles = "Vendor,Admin")]
    public async Task<ActionResult<Vendor>> GetCurrentVendor()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Forbid();
        }

        var vendor = await _vendorService.GetByUserIdAsync(userId);
        if (vendor is null)
        {
            return NotFound();
        }

        return Ok(vendor);
    }

    // Admin: create vendor
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Vendor>> Create([FromForm] VendorWithBannerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.VendorJson))
        {
            return BadRequest("Vendor payload is required.");
        }

        Vendor? vendor;
        try
        {
            vendor = JsonSerializer.Deserialize<Vendor>(request.VendorJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return BadRequest("Invalid vendor JSON.");
        }

        if (vendor is null)
        {
            return BadRequest("Vendor payload is invalid.");
        }

        // Optional banner upload
        if (request.Banner is { Length: > 0 })
        {
            await using var stream = request.Banner.OpenReadStream();
            var url = await _imageStorageService.UploadAsync(
                stream,
                request.Banner.FileName,
                request.Banner.ContentType,
                HttpContext.RequestAborted);

            vendor.BannerUrl = url;
        }

        var created = await _vendorService.CreateAsync(vendor);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // Vendor/Admin: update vendor
    [HttpPut("{id}")]
    [Authorize(Roles = "Vendor,Admin")]
    public async Task<IActionResult> Update(string id, [FromForm] VendorWithBannerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.VendorJson))
        {
            return BadRequest("Vendor payload is required.");
        }

        // Load existing vendor first (for ownership + merging)
        var existing = await _vendorService.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        Vendor? vendor;
        try
        {
            vendor = JsonSerializer.Deserialize<Vendor>(request.VendorJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return BadRequest("Invalid vendor JSON.");
        }

        if (vendor is null)
        {
            return BadRequest("Vendor payload is invalid.");
        }

        // If vendor role, ensure they only update their own vendor
        if (User.IsInRole("Vendor"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Forbid();
            }

            var currentVendor = await _vendorService.GetByUserIdAsync(userId);
            if (currentVendor is null || currentVendor.Id != id)
            {
                return Forbid();
            }

            // Ensure UserId is not changed
            vendor.UserId = currentVendor.UserId;
        }

        // Optional banner upload (overwrites existing BannerUrl if provided)
        if (request.Banner is { Length: > 0 })
        {
            await using var stream = request.Banner.OpenReadStream();
            var url = await _imageStorageService.UploadAsync(
                stream,
                request.Banner.FileName,
                request.Banner.ContentType,
                HttpContext.RequestAborted);

            vendor.BannerUrl = url;
        }
        else
        {
            // Preserve existing banner if none provided
            vendor.BannerUrl = existing.BannerUrl;
        }

        var ok = await _vendorService.UpdateAsync(id, vendor);
        if (!ok)
        {
            return NotFound();
        }

        return NoContent();
    }

    // Admin: delete vendor
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        var ok = await _vendorService.DeleteAsync(id);
        if (!ok)
        {
            return NotFound();
        }

        return NoContent();
    }
}
