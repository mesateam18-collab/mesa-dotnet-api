using Microsoft.AspNetCore.Http;

namespace MultiVendorEcommerce.Models.DTOs;

public class VendorWithBannerRequest
{
    public string VendorJson { get; set; } = string.Empty;
    public IFormFile? Banner { get; set; }
}
