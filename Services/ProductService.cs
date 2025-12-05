using MultiVendorEcommerce.Models.Entities;
using MultiVendorEcommerce.Repositories;

namespace MultiVendorEcommerce.Services;

public interface IProductService
{
    Task<IEnumerable<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(string id);
    Task<IEnumerable<Product>> GetByVendorAsync(string vendorId);
    Task<IEnumerable<Product>> GetByCategoryAsync(string categoryId);
    Task<IEnumerable<Product>> SearchAsync(string term);
    Task<Product> CreateAsync(Product product);
    Task<bool> UpdateAsync(string id, Product product);
    Task<bool> DeleteAsync(string id);
}

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IRepository<Category> _categoryRepository;

    public ProductService(IProductRepository productRepository, IRepository<Category> categoryRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<IEnumerable<Product>> GetAllAsync() => await _productRepository.GetAllAsync();

    public async Task<Product?> GetByIdAsync(string id) => await _productRepository.GetByIdAsync(id);

    public async Task<IEnumerable<Product>> GetByVendorAsync(string vendorId) =>
        await _productRepository.GetByVendorAsync(vendorId);

    public async Task<IEnumerable<Product>> GetByCategoryAsync(string categoryId) =>
        await _productRepository.GetByCategoryAsync(categoryId);

    public async Task<IEnumerable<Product>> SearchAsync(string term) =>
        await _productRepository.SearchAsync(term);

    public async Task<Product> CreateAsync(Product product)
    {
        await ValidateCategoriesAsync(product);
        product.CreatedAt = DateTime.UtcNow;
        await _productRepository.CreateAsync(product);
        return product;
    }

    public async Task<bool> UpdateAsync(string id, Product product)
    {
        var existing = await _productRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return false;
        }

        product.Id = id;
        product.UpdatedAt = DateTime.UtcNow;
        await ValidateCategoriesAsync(product);
        await _productRepository.UpdateAsync(id, product);
        return true;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var existing = await _productRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return false;
        }

        await _productRepository.DeleteAsync(id);
        return true;
    }

    private async Task ValidateCategoriesAsync(Product product)
    {
        if (product.Categories == null || product.Categories.Count == 0)
        {
            return;
        }

        var ids = product.Categories.Distinct().ToList();
        var existing = await _categoryRepository.FindAsync(c => ids.Contains(c.Id));
        var existingIds = existing.Select(c => c.Id).ToHashSet();

        var invalidIds = ids.Where(id => !existingIds.Contains(id)).ToList();
        if (invalidIds.Count > 0)
        {
            throw new InvalidOperationException("One or more category IDs are invalid.");
        }
    }
}
