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
public class ProductsController(
    IProductService productService,
    IVendorService vendorService,
    IImageStorageService imageStorageService) : ControllerBase
{
    private readonly IProductService _productService = productService;
    private readonly IVendorService _vendorService = vendorService;
    private readonly IImageStorageService _imageStorageService = imageStorageService;

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
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<Product>> Create([FromForm] CreateProductWithImagesRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Resolve vendor
        string? vendorId = request.VendorId;
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
            vendorId = vendor.Id;
        }
        else if (User.IsInRole("Admin"))
        {
            if (string.IsNullOrWhiteSpace(vendorId))
            {
                return BadRequest("VendorId is required for Admin when creating a product.");
            }
        }

        var product = new Product
        {
            Id = string.Empty,
            VendorId = vendorId ?? string.Empty,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Price = request.Price,
            SalesPrice = request.SalesPrice,
            StockQuantity = request.StockQuantity,
            stockStatus = request.StockStatus,
            ImageUrls = new List<string>(),
            Categories = ParseCategories(request.CategoriesCsv),
            Attributes = request.Attributes ?? new Dictionary<string, string>(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Upload images
        if (request.Files is { Count: > 0 })
        {
            foreach (var file in request.Files)
            {
                if (file.Length <= 0) continue;

                await using var stream = file.OpenReadStream();
                var url = await _imageStorageService.UploadAsync(
                    stream,
                    file.FileName,
                    file.ContentType,
                    HttpContext.RequestAborted);

                product.ImageUrls.Add(url);
            }
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
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Update(string id, [FromForm] CreateProductWithImagesRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Load existing product to enforce ownership and for merging
        var existing = await _productService.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        // Vendor can only modify own products; Admin can edit any
        string vendorId = existing.VendorId;
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

            vendorId = vendor.Id;
        }
        else if (User.IsInRole("Admin") && !string.IsNullOrWhiteSpace(request.VendorId))
        {
            vendorId = request.VendorId!;
        }

        var product = new Product
        {
            Id = id,
            VendorId = vendorId,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Price = request.Price,
            SalesPrice = request.SalesPrice,
            StockQuantity = request.StockQuantity,
            stockStatus = request.StockStatus,
            ImageUrls = existing.ImageUrls ?? new List<string>(),
            Categories = ParseCategories(request.CategoriesCsv),
            Attributes = request.Attributes ?? existing.Attributes ?? new Dictionary<string, string>(),
            IsActive = existing.IsActive,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        // Append any newly uploaded images
        if (request.Files is { Count: > 0 })
        {
            foreach (var file in request.Files)
            {
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

                product.ImageUrls.Add(url);
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

    private static List<string> ParseCategories(string? categoriesCsv)
    {
        if (string.IsNullOrWhiteSpace(categoriesCsv)) return new List<string>();
        return categoriesCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToList();
    }
}
