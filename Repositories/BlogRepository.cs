using MongoDB.Driver;
using MultiVendorEcommerce.Models.Entities;

namespace MultiVendorEcommerce.Repositories;

public interface IBlogRepository : IRepository<Blog>
{
}

public class BlogRepository(IMongoCollection<Blog> collection) : Repository<Blog>(collection), IBlogRepository;
