using MongoDB.Driver;
using MultiVendorEcommerce.Models.Entities;

namespace MultiVendorEcommerce.Repositories;

public interface IBlogRepository : IRepository<Blog>
{
}

public class BlogRepository : Repository<Blog>, IBlogRepository
{
    public BlogRepository(IMongoCollection<Blog> collection) : base(collection)
    {
    }
}
