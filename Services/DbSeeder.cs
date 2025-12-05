using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MultiVendorEcommerce.Models.Entities;
using MultiVendorEcommerce.Repositories;

namespace MultiVendorEcommerce.Services;

public static class DbSeeder
{
    public static async Task SeedAdminAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
        var config = provider.GetRequiredService<IConfiguration>();
        var userRepo = provider.GetRequiredService<IRepository<User>>();

        var section = config.GetSection("AdminSeed");
        var email = section["Email"];
        var username = section["Username"];
        var password = section["Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("AdminSeed configuration incomplete, skipping admin seeding.");
            return;
        }

        var existing = await userRepo.FindAsync(u => u.Email == email);
        if (existing.Any())
        {
            logger.LogInformation("Admin user already exists with email {Email}", email);
            return;
        }

        var admin = new User
        {
            Username = string.IsNullOrWhiteSpace(username) ? "admin" : username,
            Email = email,
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        };

        admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

        await userRepo.CreateAsync(admin);
        logger.LogInformation("Seeded default admin user with email {Email}", email);
    }
}
