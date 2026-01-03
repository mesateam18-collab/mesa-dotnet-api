using MultiVendorEcommerce.Models.Entities;
using MultiVendorEcommerce.Repositories;

namespace MultiVendorEcommerce.Services;

public interface IOrderService
{
    Task<IEnumerable<Order>> GetAllAsync();
    Task<Order?> GetByIdAsync(string id);
    Task<IEnumerable<Order>> GetByCustomerAsync(string customerId);
    Task<IEnumerable<Order>> GetByVendorAsync(string vendorId);
    Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status);
    Task<Order> CreateAsync(Order order);
    Task<bool> UpdateStatusAsync(string id, OrderStatus status);
    Task<bool> DeleteAsync(string id);
}

public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orderRepository;

    public OrderService(IRepository<Order> orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<IEnumerable<Order>> GetAllAsync() => await _orderRepository.GetAllAsync();

    public async Task<Order?> GetByIdAsync(string id) => await _orderRepository.GetByIdAsync(id);

    public async Task<IEnumerable<Order>> GetByCustomerAsync(string customerId) =>
        await _orderRepository.FindAsync(o => o.CustomerId == customerId);

    public async Task<IEnumerable<Order>> GetByVendorAsync(string vendorId) =>
        await _orderRepository.FindAsync(o => o.Items.Any(i => i.VendorId == vendorId));

    public async Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status) =>
        await _orderRepository.FindAsync(o => o.Status == status);

    public async Task<Order> CreateAsync(Order order)
    {
        order.CreatedAt = DateTime.UtcNow;
        await _orderRepository.CreateAsync(order);
        return order;
    }

    public async Task<bool> UpdateStatusAsync(string id, OrderStatus status)
    {
        try
        {
            var existing = await _orderRepository.GetByIdAsync(id);
            if (existing == null) return false;

            existing.Status = status;
            existing.UpdatedAt = DateTime.UtcNow;
            await _orderRepository.UpdateAsync(id, existing);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            var existing = await _orderRepository.GetByIdAsync(id);
            if (existing == null) return false;

            await _orderRepository.DeleteAsync(id);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
