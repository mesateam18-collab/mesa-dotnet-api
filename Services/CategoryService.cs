using MultiVendorEcommerce.Models.Entities;
using MultiVendorEcommerce.Repositories;

namespace MultiVendorEcommerce.Services;

public interface ICategoryService
{
    Task<IEnumerable<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(string id);
    Task<Category> CreateAsync(Category category);
    Task<bool> UpdateAsync(string id, Category category);
    Task<bool> DeleteAsync(string id);
}

public class CategoryService(IRepository<Category> categoryRepository) : ICategoryService
{
    private readonly IRepository<Category> _categoryRepository = categoryRepository;

    public async Task<IEnumerable<Category>> GetAllAsync() =>
        await _categoryRepository.GetAllAsync();

    public async Task<Category?> GetByIdAsync(string id) =>
        await _categoryRepository.GetByIdAsync(id);

    public async Task<Category> CreateAsync(Category category)
    {
        category.CreatedAt = DateTime.UtcNow;
        await _categoryRepository.CreateAsync(category);
        return category;
    }

    public async Task<bool> UpdateAsync(string id, Category category)
    {
        var existing = await _categoryRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return false;
        }

        category.Id = id;
        await _categoryRepository.UpdateAsync(id, category);
        return true;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var existing = await _categoryRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return false;
        }

        await _categoryRepository.DeleteAsync(id);
        return true;
    }
}
