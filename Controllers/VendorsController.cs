using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiVendorEcommerce.Models.Entities;
using MultiVendorEcommerce.Services;

namespace MultiVendorEcommerce.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VendorsController(IVendorService vendorService) : ControllerBase
{
    private readonly IVendorService _vendorService = vendorService;

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
    public async Task<ActionResult<Vendor>> Create(Vendor vendor)
    {
        var created = await _vendorService.CreateAsync(vendor);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // Vendor/Admin: update vendor
    [HttpPut("{id}")]
    [Authorize(Roles = "Vendor,Admin")]
    public async Task<IActionResult> Update(string id, Vendor vendor)
    {
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
