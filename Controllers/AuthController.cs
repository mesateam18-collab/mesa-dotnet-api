using Microsoft.AspNetCore.Mvc;
using MultiVendorEcommerce.Models.DTOs;
using MultiVendorEcommerce.Models.Entities;
using MultiVendorEcommerce.Services;

namespace MultiVendorEcommerce.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            Role = dto.Role ?? "Customer"
        };

        var result = await _authService.RegisterAsync(user, dto.Password);
        if (result is null)
        {
            return BadRequest("User already exists");
        }

        return Ok(new { token = result.Token, user = result.User });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto.Email, dto.Password);
        if (result is null)
        {
            return Unauthorized("Invalid credentials");
        }

        return Ok(new { token = result.Token, user = result.User });
    }
}
