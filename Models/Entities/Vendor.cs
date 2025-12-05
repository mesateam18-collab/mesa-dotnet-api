using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MultiVendorEcommerce.Models.Entities;

public class Vendor
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty; // Reference to User
    public string BusinessName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BannerUrl { get; set; }

    public decimal? Rating { get; set; }

    public string? Notice { get; set; }

    public string? Phone { get; set; }

    public string? Status { get; set; }

    public string? Email { get; set; }

    public string? location { get; set; }
    public decimal CommissionRate { get; set; }
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
