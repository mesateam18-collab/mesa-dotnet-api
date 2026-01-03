using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MultiVendorEcommerce.Models.Entities;

namespace MultiVendorEcommerce.Data;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}

public class MongoDbContext
{
    private readonly IMongoDatabase? _database;
    private readonly IMongoClient? _client;
    private readonly ILogger<MongoDbContext> _logger;
    public bool IsConnected { get; private set; }
    public string? ConnectionError { get; private set; }

    public MongoDbContext(IOptions<MongoDbSettings> settings, ILogger<MongoDbContext> logger)
    {
        _logger = logger;
        
        try
        {
            var connectionString = settings.Value.ConnectionString;
            var databaseName = settings.Value.DatabaseName;

            // Log configuration (without sensitive data)
            _logger.LogInformation("MongoDB Configuration - Database: {DatabaseName}, ConnectionString length: {Length}, HasValue: {HasValue}", 
                databaseName ?? "null", 
                connectionString?.Length ?? 0,
                !string.IsNullOrWhiteSpace(connectionString));

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                ConnectionError = "MongoDB connection string is not configured or is empty";
                _logger.LogError(ConnectionError);
                IsConnected = false;
                return;
            }

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                ConnectionError = "MongoDB database name is not configured";
                _logger.LogError(ConnectionError);
                IsConnected = false;
                return;
            }

            // Validate connection string format
            if (!connectionString.StartsWith("mongodb://", StringComparison.OrdinalIgnoreCase) && 
                !connectionString.StartsWith("mongodb+srv://", StringComparison.OrdinalIgnoreCase))
            {
                ConnectionError = $"Invalid MongoDB connection string format. Must start with 'mongodb://' or 'mongodb+srv://'. Connection string length: {connectionString.Length}";
                _logger.LogError(ConnectionError);
                IsConnected = false;
                return;
            }

            _logger.LogInformation("Attempting to connect to MongoDB. Database: {DatabaseName}", databaseName);
            
            // Validate connection string is not empty before parsing
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                ConnectionError = "MongoDB connection string is empty when attempting to create client";
                _logger.LogError(ConnectionError);
                IsConnected = false;
                return;
            }
            
            // Log first 20 characters of connection string for debugging (without exposing credentials)
            var connectionStringPreview = connectionString.Length > 20 
                ? connectionString.Substring(0, 20) + "..." 
                : connectionString;
            _logger.LogInformation("MongoDB connection string preview: {Preview}", connectionStringPreview);
            
            MongoClientSettings mongoSettings;
            try
            {
                mongoSettings = MongoClientSettings.FromConnectionString(connectionString);
            }
            catch (ArgumentException ex)
            {
                ConnectionError = $"MongoDB connection string is invalid: {ex.Message}. Connection string length: {connectionString.Length}";
                _logger.LogError(ex, ConnectionError);
                IsConnected = false;
                return;
            }
            mongoSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);
            mongoSettings.ConnectTimeout = TimeSpan.FromSeconds(10);
            mongoSettings.SocketTimeout = TimeSpan.FromSeconds(10);
            mongoSettings.RetryWrites = true;
            mongoSettings.RetryReads = true;
            
            _client = new MongoClient(mongoSettings);
            _database = _client.GetDatabase(databaseName);
            
            // Test connection by listing collections (with short timeout)
            _database.ListCollectionNames().FirstOrDefault();
            
            IsConnected = true;
            _logger.LogInformation("Successfully connected to MongoDB database: {DatabaseName}", databaseName);
        }
        catch (TimeoutException ex)
        {
            ConnectionError = $"MongoDB connection timeout: {ex.Message}";
            _logger.LogError(ex, "MongoDB connection timeout - check network/firewall settings");
            IsConnected = false;
        }
        catch (MongoAuthenticationException ex)
        {
            ConnectionError = $"MongoDB authentication failed: {ex.Message}";
            _logger.LogError(ex, "MongoDB authentication failed - check credentials");
            IsConnected = false;
        }
        catch (MongoConnectionException ex)
        {
            ConnectionError = $"MongoDB connection failed: {ex.Message}";
            _logger.LogError(ex, "MongoDB connection failed - server may be unreachable");
            IsConnected = false;
        }
        catch (ArgumentException ex)
        {
            // This catches "List of configured name servers must not be empty" error
            ConnectionError = $"MongoDB connection string is invalid: {ex.Message}. Please check your connection string format.";
            _logger.LogError(ex, "MongoDB connection string validation failed. Connection string length: {Length}", 
                settings.Value.ConnectionString?.Length ?? 0);
            IsConnected = false;
        }
        catch (Exception ex)
        {
            ConnectionError = $"MongoDB error: {ex.Message}";
            _logger.LogError(ex, "Unexpected MongoDB error");
            IsConnected = false;
        }
    }

    private void EnsureConnected()
    {
        if (!IsConnected || _database == null)
        {
            throw new InvalidOperationException($"MongoDB is not connected. {ConnectionError}");
        }
    }

    public IMongoCollection<User> Users { get { EnsureConnected(); return _database!.GetCollection<User>("Users"); } }
    public IMongoCollection<Vendor> Vendors { get { EnsureConnected(); return _database!.GetCollection<Vendor>("Vendors"); } }
    public IMongoCollection<Category> Categories { get { EnsureConnected(); return _database!.GetCollection<Category>("Categories"); } }
    public IMongoCollection<Product> Products { get { EnsureConnected(); return _database!.GetCollection<Product>("Products"); } }
    public IMongoCollection<Order> Orders { get { EnsureConnected(); return _database!.GetCollection<Order>("Orders"); } }
    public IMongoCollection<Blog> Blogs { get { EnsureConnected(); return _database!.GetCollection<Blog>("Blogs"); } }
    public IMongoCollection<QuickCheck> QuickChecks { get { EnsureConnected(); return _database!.GetCollection<QuickCheck>("QuickChecks"); } }
}
