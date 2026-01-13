using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
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

public class Repository<T>(IMongoCollection<T> collection) : IRepository<T>
    where T : class
{
    protected readonly IMongoCollection<T> Collection = collection;
    private static readonly PropertyInfo? IdProperty = typeof(T).GetProperty("Id");

    public async Task<IEnumerable<T>> GetAllAsync() =>
        await Collection.Find(_ => true).ToListAsync();

    public async Task<T?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }
        
        // Use property-based filter which allows MongoDB serializer to handle ObjectId conversion
        if (IdProperty != null)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, IdProperty);
            var constant = Expression.Constant(id);
            var equals = Expression.Equal(property, constant);
            var lambda = Expression.Lambda<Func<T, bool>>(equals, parameter);
            
            return await Collection.Find(lambda).FirstOrDefaultAsync();
        }
        
        // Fallback to _id field filter
        if (ObjectId.TryParse(id, out var objectId))
        {
            var filter = Builders<T>.Filter.Eq("_id", objectId);
            return await Collection.Find(filter).FirstOrDefaultAsync();
        }
        
        var stringFilter = Builders<T>.Filter.Eq("_id", id);
        return await Collection.Find(stringFilter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> filter) =>
        await Collection.Find(filter).ToListAsync();

    public async Task CreateAsync(T entity)
    {
        await Collection.InsertOneAsync(entity);
    }

    public async Task UpdateAsync(string id, T entity)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("ID cannot be null or empty", nameof(id));
        }
        
        // Ensure the entity has the correct ID set
        if (IdProperty != null && IdProperty.CanWrite)
        {
            IdProperty.SetValue(entity, id);
        }
        
        // Use property-based filter which allows MongoDB serializer to handle ObjectId conversion
        FilterDefinition<T> filter;
        if (IdProperty != null)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, IdProperty);
            var constant = Expression.Constant(id);
            var equals = Expression.Equal(property, constant);
            var lambda = Expression.Lambda<Func<T, bool>>(equals, parameter);
            filter = Builders<T>.Filter.Where(lambda);
        }
        else if (ObjectId.TryParse(id, out var objectId))
        {
            filter = Builders<T>.Filter.Eq("_id", objectId);
        }
        else
        {
            filter = Builders<T>.Filter.Eq("_id", id);
        }
        
        var result = await Collection.ReplaceOneAsync(filter, entity);
        if (result.MatchedCount == 0)
        {
            throw new InvalidOperationException($"Entity with ID '{id}' not found for update");
        }
    }

    public async Task DeleteAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("ID cannot be null or empty", nameof(id));
        }
        
        // Use property-based filter which allows MongoDB serializer to handle ObjectId conversion
        FilterDefinition<T> filter;
        if (IdProperty != null)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, IdProperty);
            var constant = Expression.Constant(id);
            var equals = Expression.Equal(property, constant);
            var lambda = Expression.Lambda<Func<T, bool>>(equals, parameter);
            filter = Builders<T>.Filter.Where(lambda);
        }
        else if (ObjectId.TryParse(id, out var objectId))
        {
            filter = Builders<T>.Filter.Eq("_id", objectId);
        }
        else
        {
            filter = Builders<T>.Filter.Eq("_id", id);
        }
        
        var result = await Collection.DeleteOneAsync(filter);
        if (result.DeletedCount == 0)
        {
            throw new InvalidOperationException($"Entity with ID '{id}' not found for deletion");
        }
    }
}
