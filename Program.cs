using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MultiVendorEcommerce.Data;
using MultiVendorEcommerce.Models.Entities;
using MultiVendorEcommerce.Repositories;
using MultiVendorEcommerce.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.Configure<CloudflareR2Settings>(
    builder.Configuration.GetSection("CloudflareR2"));
builder.Services.AddSingleton<MongoDbContext>();

// Repositories
builder.Services.AddScoped<IRepository<User>>(sp =>
    new Repository<User>(sp.GetRequiredService<MongoDbContext>().Users));
builder.Services.AddScoped<IRepository<Vendor>>(sp =>
    new Repository<Vendor>(sp.GetRequiredService<MongoDbContext>().Vendors));
builder.Services.AddScoped<IRepository<Category>>(sp =>
    new Repository<Category>(sp.GetRequiredService<MongoDbContext>().Categories));
builder.Services.AddScoped<IRepository<Product>>(sp =>
    new Repository<Product>(sp.GetRequiredService<MongoDbContext>().Products));
builder.Services.AddScoped<IRepository<Order>>(sp =>
    new Repository<Order>(sp.GetRequiredService<MongoDbContext>().Orders));
builder.Services.AddScoped<IProductRepository>(sp =>
    new ProductRepository(sp.GetRequiredService<MongoDbContext>().Products));
builder.Services.AddScoped<IBlogRepository>(sp =>
    new BlogRepository(sp.GetRequiredService<MongoDbContext>().Blogs));

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IVendorService, VendorService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddSingleton<IImageStorageService, R2ImageStorageService>();

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Multi-Vendor E-commerce API",
        Version = "v1"
    });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter 'Bearer {token}'",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });
});

// Authentication
var jwtSection = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSection["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"]
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Seed initial data (e.g., default admin user)
await DbSeeder.SeedAdminAsync(app.Services);

// Middleware pipeline
app.UseSwagger();
app.UseSwaggerUI();
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapGet("/health", () => "OK");


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
