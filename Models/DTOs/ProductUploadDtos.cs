using Microsoft.AspNetCore.Http;

namespace MultiVendorEcommerce.Models.DTOs;

public class CreateProductWithImagesRequest
{
    public string ProductJson { get; set; } = string.Empty;
    public List<IFormFile> Files { get; set; } = new();
}
