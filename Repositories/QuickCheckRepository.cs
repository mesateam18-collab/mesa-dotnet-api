using MongoDB.Driver;
using MultiVendorEcommerce.Models.Entities;

namespace MultiVendorEcommerce.Repositories;

public interface IQuickCheckRepository : IRepository<QuickCheck>
{
}

public class QuickCheckRepository : Repository<QuickCheck>, IQuickCheckRepository
{
    public QuickCheckRepository(IMongoCollection<QuickCheck> collection) : base(collection)
    {
    }
}

