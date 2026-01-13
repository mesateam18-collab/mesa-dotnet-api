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

public class AuthService(IRepository<User> userRepository, IConfiguration configuration)
    : IAuthService
{
    public async Task<AuthResult?> RegisterAsync(User user, string password)
    {
        var existing = await userRepository.FindAsync(u => u.Email == user.Email);
        if (existing.Any())
        {
            return null;
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        user.CreatedAt = DateTime.UtcNow;

        await userRepository.CreateAsync(user);
        var token = GenerateJwtToken(user);
        return new AuthResult(token, user);
    }

    public async Task<AuthResult?> LoginAsync(string email, string password)
    {
        var users = await userRepository.FindAsync(u => u.Email == email);
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
        var jwtSection = configuration.GetSection("Jwt");
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
