using MultiVendorEcommerce.Models.Entities;
using MultiVendorEcommerce.Repositories;

namespace MultiVendorEcommerce.Services;

public interface IVendorService
{
    Task<IEnumerable<Vendor>> GetAllAsync();
    Task<Vendor?> GetByIdAsync(string id);
    Task<Vendor?> GetByUserIdAsync(string userId);
    Task<Vendor> CreateAsync(Vendor vendor);
    Task<bool> UpdateAsync(string id, Vendor vendor);
    Task<bool> DeleteAsync(string id);
}

public class VendorService(IRepository<Vendor> vendorRepository) : IVendorService
{
    private readonly IRepository<Vendor> _vendorRepository = vendorRepository;

    public async Task<IEnumerable<Vendor>> GetAllAsync() =>
        await _vendorRepository.GetAllAsync();

    public async Task<Vendor?> GetByIdAsync(string id) =>
        await _vendorRepository.GetByIdAsync(id);

    public async Task<Vendor?> GetByUserIdAsync(string userId)
    {
        var vendors = await _vendorRepository.FindAsync(v => v.UserId == userId);
        return vendors.FirstOrDefault();
    }

    public async Task<Vendor> CreateAsync(Vendor vendor)
    {
        vendor.CreatedAt = DateTime.UtcNow;
        await _vendorRepository.CreateAsync(vendor);
        return vendor;
    }

    public async Task<bool> UpdateAsync(string id, Vendor vendor)
    {
        var existing = await _vendorRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return false;
        }

        vendor.Id = id;
        await _vendorRepository.UpdateAsync(id, vendor);
        return true;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var existing = await _vendorRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return false;
        }

        await _vendorRepository.DeleteAsync(id);
        return true;
    }
}
