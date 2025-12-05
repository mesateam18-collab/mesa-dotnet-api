using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MultiVendorEcommerce.Models.Entities;
using MultiVendorEcommerce.Repositories;

namespace MultiVendorEcommerce.Services;

public record AuthResult(string Token, User User);

public interface IAuthService
{
    Task<AuthResult?> RegisterAsync(User user, string password);
    Task<AuthResult?> LoginAsync(string email, string password);
}

public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly IConfiguration _configuration;

    public AuthService(IRepository<User> userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    public async Task<AuthResult?> RegisterAsync(User user, string password)
    {
        var existing = await _userRepository.FindAsync(u => u.Email == user.Email);
        if (existing.Any())
        {
            return null;
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        user.CreatedAt = DateTime.UtcNow;

        await _userRepository.CreateAsync(user);
        var token = GenerateJwtToken(user);
        return new AuthResult(token, user);
    }

    public async Task<AuthResult?> LoginAsync(string email, string password)
    {
        var users = await _userRepository.FindAsync(u => u.Email == email);
        var user = users.FirstOrDefault();
        if (user == null)
        {
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return null;
        }

        var token = GenerateJwtToken(user);
        return new AuthResult(token, user);
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var key = Encoding.ASCII.GetBytes(jwtSection["Key"]!);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature),
            Issuer = jwtSection["Issuer"],
            Audience = jwtSection["Audience"]
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }
}
