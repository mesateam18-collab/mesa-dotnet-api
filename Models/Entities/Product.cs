using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MultiVendorEcommerce.Models.Entities;

public class Product
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public string VendorId { get; set; } = string.Empty; // Reference to Vendor
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal SalesPrice { get; set; }
    
    public int StockQuantity { get; set; }

    public bool stockStatus { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public Dictionary<string, string> Attributes { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
