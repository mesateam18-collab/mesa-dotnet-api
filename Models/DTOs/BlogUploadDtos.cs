using Microsoft.AspNetCore.Http;

namespace MultiVendorEcommerce.Models.DTOs;

public class BlogWithImagesRequest
{
    public string BlogJson { get; set; } = string.Empty;

    // All uploaded images; first will be used as ImageUrl, the rest as ContentImages
    public List<IFormFile> Files { get; set; } = new();
}
