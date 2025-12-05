using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiVendorEcommerce.Models.Entities;
using MultiVendorEcommerce.Services;

namespace MultiVendorEcommerce.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController(IProductService productService, IVendorService vendorService) : ControllerBase
{
    private readonly IProductService _productService = productService;
    private readonly IVendorService _vendorService = vendorService;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll()
    {
        var products = await _productService.GetAllAsync();
        return Ok(products);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Product>> GetById(string id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Product>>> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest("Search term is required");
        }

        var products = await _productService.SearchAsync(q);
        return Ok(products);
    }

    [HttpGet("vendor/{vendorId}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Product>>> GetByVendor(string vendorId)
    {
        var products = await _productService.GetByVendorAsync(vendorId);
        return Ok(products);
    }

    [HttpGet("category/{categoryId}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Product>>> GetByCategory(string categoryId)
    {
        var products = await _productService.GetByCategoryAsync(categoryId);
        return Ok(products);
    }

    [HttpPost]
    [Authorize(Roles = "Vendor,Admin")]
    public async Task<ActionResult<Product>> Create(Product product)
    {
        // Resolve vendor for current user (Vendor role only)
        if (User.IsInRole("Vendor"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Forbid();
            }

            var vendor = await _vendorService.GetByUserIdAsync(userId);
            if (vendor is null)
            {
                return Forbid("Vendor profile not found for current user");
            }

            product.VendorId = vendor.Id;
        }

        try
        {
            var created = await _productService.CreateAsync(product);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Vendor,Admin")]
    public async Task<IActionResult> Update(string id, Product product)
    {
        // Load existing product to enforce ownership
        var existing = await _productService.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        // Vendor can only modify own products; Admin can edit any
        if (User.IsInRole("Vendor"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Forbid();
            }

            var vendor = await _vendorService.GetByUserIdAsync(userId);
            if (vendor is null || existing.VendorId != vendor.Id)
            {
                return Forbid();
            }

            // Ensure VendorId cannot be changed by the body
            product.VendorId = vendor.Id;
        }
        else if (User.IsInRole("Admin"))
        {
            // Admin can choose to leave VendorId as-is if not set in body
            if (string.IsNullOrWhiteSpace(product.VendorId))
            {
                product.VendorId = existing.VendorId;
            }
        }

        try
        {
            var ok = await _productService.UpdateAsync(id, product);
            if (!ok)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Vendor,Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        var existing = await _productService.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        // Vendor can only delete own products; Admin can delete any
        if (User.IsInRole("Vendor"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Forbid();
            }

            var vendor = await _vendorService.GetByUserIdAsync(userId);
            if (vendor is null || existing.VendorId != vendor.Id)
            {
                return Forbid();
            }
        }

        var ok = await _productService.DeleteAsync(id);
        if (!ok)
        {
            return NotFound();
        }

        return NoContent();
    }
}
