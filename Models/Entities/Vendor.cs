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
    public string? LogoUrl { get; set; }
    public decimal CommissionRate { get; set; }
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
