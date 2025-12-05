using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiVendorEcommerce.Models.Entities;
using MultiVendorEcommerce.Repositories;
using MultiVendorEcommerce.Services;

namespace MultiVendorEcommerce.Controllers;

[ApiController]
[Route("api/products/{productId}/images")]
[Authorize(Roles = "Vendor,Admin")]
public class ProductImagesController(
    IProductRepository productRepository,
    IImageStorageService imageStorageService) : ControllerBase
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IImageStorageService _imageStorageService = imageStorageService;

    [HttpPost]
    [RequestSizeLimit(10_000_000)] // ~10MB total
    public async Task<ActionResult<Product>> Upload(string productId, [FromForm] List<IFormFile> files)
    {
        if (files is not { Count: > 0 })
        {
            return BadRequest("At least one image file is required.");
        }

        var product = await _productRepository.GetByIdAsync(productId);
        if (product is null)
        {
            return NotFound();
        }

        foreach (var file in files)
        {
            if (file.Length <= 0)
            {
                continue;
            }

            await using var stream = file.OpenReadStream();
            var url = await _imageStorageService.UploadAsync(stream, file.FileName, file.ContentType, HttpContext.RequestAborted);
            product.ImageUrls.Add(url);
        }

        await _productRepository.UpdateAsync(productId, product);

        return Ok(product);
    }
}
