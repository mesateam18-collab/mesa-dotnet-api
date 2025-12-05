using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MultiVendorEcommerce.Models.Entities;

public class Order
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public string CustomerId { get; set; } = string.Empty; // Reference to User
    public List<OrderItem> Items { get; set; } = new();
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public ShippingAddress ShippingAddress { get; set; } = new();
    public PaymentInfo PaymentInfo { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class OrderItem
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string VendorId { get; set; } = string.Empty;
}

public class ShippingAddress
{
    public string FullName { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

public class PaymentInfo
{
    public string PaymentMethod { get; set; } = string.Empty; // e.g. CreditCard, PayPal
    public string? TransactionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
}

public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered,
    Cancelled,
    Refunded
}
