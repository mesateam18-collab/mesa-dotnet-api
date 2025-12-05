using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiVendorEcommerce.Models.Entities;
using MultiVendorEcommerce.Services;

namespace MultiVendorEcommerce.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    private readonly ICategoryService _categoryService = categoryService;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Category>>> GetAll()
    {
        var categories = await _categoryService.GetAllAsync();
        return Ok(categories);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Category>> GetById(string id)
    {
        var category = await _categoryService.GetByIdAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        return Ok(category);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")] 
    public async Task<ActionResult<Category>> Create(Category category)
    {
        var created = await _categoryService.CreateAsync(category);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")] 
    public async Task<IActionResult> Update(string id, Category category)
    {
        var ok = await _categoryService.UpdateAsync(id, category);
        if (!ok)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")] 
    public async Task<IActionResult> Delete(string id)
    {
        var ok = await _categoryService.DeleteAsync(id);
        if (!ok)
        {
            return NotFound();
        }

        return NoContent();
    }
}
