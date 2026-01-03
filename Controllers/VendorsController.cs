using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiVendorEcommerce.Models.DTOs;
using MultiVendorEcommerce.Models.Entities;
using MultiVendorEcommerce.Services;

namespace MultiVendorEcommerce.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VendorsController(IVendorService vendorService, IImageStorageService imageStorageService, IAuthService authService) : ControllerBase
{
    private readonly IVendorService _vendorService = vendorService;
    private readonly IImageStorageService _imageStorageService = imageStorageService;
    private readonly IAuthService _authService = authService;

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
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        string userId;

        // If VendorEmail and VendorPassword are provided, create a new User account automatically
        if (!string.IsNullOrWhiteSpace(request.VendorEmail) && !string.IsNullOrWhiteSpace(request.VendorPassword))
        {
            var newUser = new User
            {
                Username = request.BusinessName.Trim(),
                Email = request.VendorEmail.Trim(),
                Role = UserRole.Vendor.ToString()
            };

            var authResult = await _authService.RegisterAsync(newUser, request.VendorPassword);
            if (authResult == null)
            {
                return BadRequest("A user with this email already exists.");
            }

            userId = authResult.User.Id;
        }
        else if (!string.IsNullOrWhiteSpace(request.UserId))
        {
            // Use provided UserId if no email/password provided
            userId = request.UserId;
        }
        else
        {
            return BadRequest("Either UserId or VendorEmail and VendorPassword must be provided when creating a vendor.");
        }

        var vendor = new Vendor
        {
            Id = string.Empty,
            UserId = userId,
            BusinessName = request.BusinessName.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Notice = string.IsNullOrWhiteSpace(request.Notice) ? null : request.Notice.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? null : request.Status.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim(),
            CommissionRate = request.CommissionRate,
            Rating = request.Rating,
            IsApproved = request.IsApproved,
            BannerUrl = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

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
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existing = await _vendorService.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        // If vendor role, ensure they only update their own vendor
        string userId = request.UserId;
        if (User.IsInRole("Vendor"))
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Forbid();
            }

            var currentVendor = await _vendorService.GetByUserIdAsync(currentUserId);
            if (currentVendor is null || currentVendor.Id != id)
            {
                return Forbid();
            }

            // Ensure UserId is not changed
            userId = currentVendor.UserId;
        }
        else if (User.IsInRole("Admin") && string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest("UserId is required when updating a vendor.");
        }

        var vendor = new Vendor
        {
            Id = id,
            UserId = userId,
            BusinessName = request.BusinessName.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Notice = string.IsNullOrWhiteSpace(request.Notice) ? null : request.Notice.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? null : request.Status.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim(),
            CommissionRate = request.CommissionRate,
            Rating = request.Rating,
            IsApproved = request.IsApproved,
            BannerUrl = existing.BannerUrl,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

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
