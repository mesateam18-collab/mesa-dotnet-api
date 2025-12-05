using MongoDB.Driver;
using MultiVendorEcommerce.Models.Entities;

namespace MultiVendorEcommerce.Repositories;

public interface IProductRepository : IRepository<Product>
{
    Task<IEnumerable<Product>> GetByVendorAsync(string vendorId);
    Task<IEnumerable<Product>> GetByCategoryAsync(string categoryId);
    Task<IEnumerable<Product>> SearchAsync(string searchTerm);
}

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(IMongoCollection<Product> collection) : base(collection)
    {
    }

    public async Task<IEnumerable<Product>> GetByVendorAsync(string vendorId)
    {
        return await Collection.Find(p => p.VendorId == vendorId).ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetByCategoryAsync(string categoryId)
    {
        return await Collection.Find(p => p.Categories.Contains(categoryId)).ToListAsync();
    }

    public async Task<IEnumerable<Product>> SearchAsync(string searchTerm)
    {
        var filter = Builders<Product>.Filter.Text(searchTerm);
        return await Collection.Find(filter).ToListAsync();
    }
}
