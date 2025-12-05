using System.Linq.Expressions;
using MongoDB.Driver;

namespace MultiVendorEcommerce.Repositories;

public interface IRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(string id);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> filter);
    Task CreateAsync(T entity);
    Task UpdateAsync(string id, T entity);
    Task DeleteAsync(string id);
}

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly IMongoCollection<T> Collection;

    public Repository(IMongoCollection<T> collection)
    {
        Collection = collection;
    }

    public async Task<IEnumerable<T>> GetAllAsync() =>
        await Collection.Find(_ => true).ToListAsync();

    public async Task<T?> GetByIdAsync(string id)
    {
        var filter = Builders<T>.Filter.Eq("_id", id);
        return await Collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> filter) =>
        await Collection.Find(filter).ToListAsync();

    public async Task CreateAsync(T entity) =>
        await Collection.InsertOneAsync(entity);

    public async Task UpdateAsync(string id, T entity)
    {
        var filter = Builders<T>.Filter.Eq("_id", id);
        await Collection.ReplaceOneAsync(filter, entity);
    }

    public async Task DeleteAsync(string id)
    {
        var filter = Builders<T>.Filter.Eq("_id", id);
        await Collection.DeleteOneAsync(filter);
    }
}
