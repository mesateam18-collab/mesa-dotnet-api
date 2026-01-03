using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiVendorEcommerce.Models.Entities;
using MultiVendorEcommerce.Services;

namespace MultiVendorEcommerce.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    private readonly IOrderService _orderService = orderService;

    // Admin: get all orders
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<Order>>> GetAll([FromQuery] string? status)
    {
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
        {
            var filtered = await _orderService.GetByStatusAsync(orderStatus);
            return Ok(filtered);
        }

        var orders = await _orderService.GetAllAsync();
        return Ok(orders);
    }

    // Get order by id
    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetById(string id)
    {
        var order = await _orderService.GetByIdAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        // Customers can only view their own orders
        if (User.IsInRole("Customer"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (order.CustomerId != userId)
            {
                return Forbid();
            }
        }

        return Ok(order);
    }

    // Customer: get own orders
    [HttpGet("my-orders")]
    public async Task<ActionResult<IEnumerable<Order>>> GetMyOrders()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Forbid();
        }

        var orders = await _orderService.GetByCustomerAsync(userId);
        return Ok(orders);
    }

    // Vendor: get orders containing vendor's products
    [HttpGet("vendor/{vendorId}")]
    [Authorize(Roles = "Vendor,Admin")]
    public async Task<ActionResult<IEnumerable<Order>>> GetByVendor(string vendorId)
    {
        var orders = await _orderService.GetByVendorAsync(vendorId);
        return Ok(orders);
    }

    // Create order (typically from customer checkout)
    [HttpPost]
    public async Task<ActionResult<Order>> Create(Order order)
    {
        // Set customer ID from auth
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            order.CustomerId = userId;
        }

        var created = await _orderService.CreateAsync(order);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // Update order status
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Vendor,Admin")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateStatusRequest request)
    {
        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var status))
        {
            return BadRequest("Invalid status value");
        }

        var ok = await _orderService.UpdateStatusAsync(id, status);
        if (!ok)
        {
            return NotFound();
        }

        return NoContent();
    }

    // Admin: delete order
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        var ok = await _orderService.DeleteAsync(id);
        if (!ok)
        {
            return NotFound();
        }

        return NoContent();
    }
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
