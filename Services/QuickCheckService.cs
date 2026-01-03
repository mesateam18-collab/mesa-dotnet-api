using MultiVendorEcommerce.Models.Entities;
using MultiVendorEcommerce.Repositories;

namespace MultiVendorEcommerce.Services;

public interface IQuickCheckService
{
    Task<IEnumerable<QuickCheck>> GetAllAsync();
    Task<QuickCheck?> GetByIdAsync(string id);
    Task<QuickCheck> CreateAsync(QuickCheck quickCheck);
    Task<bool> UpdateAsync(string id, QuickCheck quickCheck);
    Task<bool> DeleteAsync(string id);
}

public class QuickCheckService : IQuickCheckService
{
    private readonly IQuickCheckRepository _quickCheckRepository;

    public QuickCheckService(IQuickCheckRepository quickCheckRepository)
    {
        _quickCheckRepository = quickCheckRepository;
    }

    public async Task<IEnumerable<QuickCheck>> GetAllAsync() => await _quickCheckRepository.GetAllAsync();

    public async Task<QuickCheck?> GetByIdAsync(string id) => await _quickCheckRepository.GetByIdAsync(id);

    public async Task<QuickCheck> CreateAsync(QuickCheck quickCheck)
    {
        quickCheck.CreatedAt = DateTime.UtcNow;
        await _quickCheckRepository.CreateAsync(quickCheck);
        return quickCheck;
    }

    public async Task<bool> UpdateAsync(string id, QuickCheck quickCheck)
    {
        try
        {
            var existing = await _quickCheckRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return false;
            }

            quickCheck.Id = id;
            quickCheck.UpdatedAt = DateTime.UtcNow;
            await _quickCheckRepository.UpdateAsync(id, quickCheck);
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
            var existing = await _quickCheckRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return false;
            }

            await _quickCheckRepository.DeleteAsync(id);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

