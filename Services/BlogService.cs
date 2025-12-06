using MultiVendorEcommerce.Models.Entities;
using MultiVendorEcommerce.Repositories;

namespace MultiVendorEcommerce.Services;

public interface IBlogService
{
    Task<IEnumerable<Blog>> GetAllAsync();
    Task<Blog?> GetByIdAsync(string id);
    Task<Blog> CreateAsync(Blog blog);
    Task<bool> UpdateAsync(string id, Blog blog);
    Task<bool> DeleteAsync(string id);
}

public class BlogService : IBlogService
{
    private readonly IBlogRepository _blogRepository;

    public BlogService(IBlogRepository blogRepository)
    {
        _blogRepository = blogRepository;
    }

    public async Task<IEnumerable<Blog>> GetAllAsync() => await _blogRepository.GetAllAsync();

    public async Task<Blog?> GetByIdAsync(string id) => await _blogRepository.GetByIdAsync(id);

    public async Task<Blog> CreateAsync(Blog blog)
    {
        blog.CreatedAt = DateTime.UtcNow;
        await _blogRepository.CreateAsync(blog);
        return blog;
    }

    public async Task<bool> UpdateAsync(string id, Blog blog)
    {
        var existing = await _blogRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return false;
        }

        blog.Id = id;
        blog.UpdatedAt = DateTime.UtcNow;
        await _blogRepository.UpdateAsync(id, blog);
        return true;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var existing = await _blogRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return false;
        }

        await _blogRepository.DeleteAsync(id);
        return true;
    }
}
