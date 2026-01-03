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
builder.Services.AddScoped<IQuickCheckRepository>(sp =>
    new QuickCheckRepository(sp.GetRequiredService<MongoDbContext>().QuickChecks));

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IVendorService, VendorService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddScoped<IQuickCheckService, QuickCheckService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddSingleton<IImageStorageService, R2ImageStorageService>();

// Controllers with JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// CORS - Allow Flutter Web App
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFlutterWeb", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Multi-Vendor E-commerce API",
        Version = "v1",
        Description = "API for managing multi-vendor e-commerce platform"
    });

    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

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

// Check MongoDB connection status (don't crash if not connected)
var mongoContext = app.Services.GetRequiredService<MongoDbContext>();
if (!mongoContext.IsConnected)
{
    app.Logger.LogWarning("⚠️ Application starting WITHOUT MongoDB connection: {Error}", mongoContext.ConnectionError);
    app.Logger.LogWarning("⚠️ API endpoints requiring database access will return errors until MongoDB is available");
}
else
{
    app.Logger.LogInformation("✅ MongoDB connected successfully");
}

// Middleware pipeline
app.UseSwagger();
app.UseSwaggerUI();
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Health check with MongoDB status
app.MapGet("/health", (MongoDbContext db) => new
{
    status = "OK",
    mongoDb = new
    {
        connected = db.IsConnected,
        error = db.ConnectionError
    },
    timestamp = DateTime.UtcNow
});

// Enable CORS
app.UseCors("AllowFlutterWeb");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
