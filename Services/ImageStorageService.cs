using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Options;

namespace MultiVendorEcommerce.Services;

public class CloudflareR2Settings
{
    public string AccountId { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    // Base URL used to serve images (e.g., Cloudflare R2 public bucket URL or CDN domain)
    public string PublicBaseUrl { get; set; } = string.Empty;
}

public interface IImageStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default, string? folder = null);
}

public class R2ImageStorageService : IImageStorageService
{
    private readonly CloudflareR2Settings _settings;
    private readonly IAmazonS3? _s3;
    private readonly bool _isConfigured;

    public R2ImageStorageService(IOptions<CloudflareR2Settings> options)
    {
        _settings = options.Value;

        // Validate required settings
        _isConfigured = !string.IsNullOrWhiteSpace(_settings.AccountId) &&
                        !string.IsNullOrWhiteSpace(_settings.AccessKeyId) &&
                        !string.IsNullOrWhiteSpace(_settings.SecretAccessKey) &&
                        !string.IsNullOrWhiteSpace(_settings.BucketName);

        if (!_isConfigured)
        {
            // Log warning but don't throw - allow app to start without R2 configured
            Console.WriteLine("WARNING: Cloudflare R2 is not configured. Image uploads will fail. Please configure CloudflareR2Settings in appsettings.json.");
            return;
        }

        // Warn if PublicBaseUrl is not set or is using the storage endpoint (not publicly accessible)
        if (string.IsNullOrWhiteSpace(_settings.PublicBaseUrl))
        {
            Console.WriteLine("⚠️ WARNING: PublicBaseUrl is not configured. Images will be uploaded but URLs will not be publicly accessible.");
            Console.WriteLine("⚠️ Options:");
            Console.WriteLine("   1. Enable 'Public Development URL' in R2 bucket settings (gives you pub-xxxxx.r2.dev URL)");
            Console.WriteLine("   2. Set up a custom domain in R2");
            Console.WriteLine("   3. Use Cloudflare Workers proxy");
            Console.WriteLine("⚠️ See R2_PUBLIC_ACCESS_SETUP.md for instructions.");
        }
        else if (_settings.PublicBaseUrl.Contains(".r2.cloudflarestorage.com"))
        {
            Console.WriteLine("⚠️ WARNING: PublicBaseUrl is using R2 storage endpoint which is NOT publicly accessible.");
            Console.WriteLine("⚠️ You need to use one of these instead:");
            Console.WriteLine("   1. R2 Public Development URL (pub-xxxxx.r2.dev) - Enable in bucket settings");
            Console.WriteLine("   2. Custom domain (e.g., https://cdn.yourdomain.com)");
            Console.WriteLine("   3. Cloudflare Workers proxy URL");
            Console.WriteLine("⚠️ See R2_PUBLIC_ACCESS_SETUP.md for instructions.");
        }
        else if (_settings.PublicBaseUrl.Contains(".r2.dev"))
        {
            Console.WriteLine("✅ Using R2 Public Development URL - images will be publicly accessible");
        }
        else
        {
            Console.WriteLine("✅ Using custom domain/Workers URL for public access");
        }

        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{_settings.AccountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true,
            AuthenticationRegion = "auto",
            UseHttp = false
        };

        var creds = new BasicAWSCredentials(_settings.AccessKeyId, _settings.SecretAccessKey);
        _s3 = new AmazonS3Client(creds, config);
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default, string? folder = null)
    {
        if (!_isConfigured || _s3 == null)
        {
            throw new InvalidOperationException("Cloudflare R2 is not configured. Please set AccountId, AccessKeyId, SecretAccessKey, and BucketName in CloudflareR2Settings.");
        }

        // Use provided folder or default to "products"
        var folderPath = !string.IsNullOrWhiteSpace(folder) ? folder.TrimEnd('/') : "products";
        var key = $"{folderPath}/{Guid.NewGuid()}-{fileName}";

        // Read entire stream into memory to avoid streaming signature issues with R2
        // R2 doesn't support STREAMING-AWS4-HMAC-SHA256-PAYLOAD signatures
        byte[] fileBytes;
        using (var memoryStream = new MemoryStream())
        {
            await stream.CopyToAsync(memoryStream, cancellationToken);
            fileBytes = memoryStream.ToArray();
        }

        // Create a new MemoryStream from the bytes
        using var fileStream = new MemoryStream(fileBytes, 0, fileBytes.Length, writable: false);
        fileStream.Position = 0;

        // According to Cloudflare R2 documentation:
        // https://developers.cloudflare.com/r2/examples/aws/aws-sdk-net/
        // DisablePayloadSigning and DisableDefaultChecksumValidation must be set to true
        // because R2 does not support the Streaming SigV4 implementation used by AWSSDK.S3
        var request = new PutObjectRequest
        {
            BucketName = _settings.BucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = contentType,
            CannedACL = S3CannedACL.PublicRead,
            DisablePayloadSigning = true, // Required for R2 compatibility
            DisableDefaultChecksumValidation = true // Required for R2 compatibility
        };

        try
        {
            await _s3.PutObjectAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to upload image to R2: {ex.Message}", ex);
        }


        // R2 provides public URLs through:
        // 1. Public Development URL (r2.dev) - Enable in bucket settings, format: https://pub-xxxxx.r2.dev
        // 2. Custom Domain - Add your domain in bucket settings, format: https://cdn.yourdomain.com
        // 3. Cloudflare Workers - Proxy requests through a Worker
        //
        // The storage endpoint (r2.cloudflarestorage.com) is NOT publicly accessible
        
        if (!string.IsNullOrWhiteSpace(_settings.PublicBaseUrl))
        {
            // Use public URL (r2.dev, custom domain, or Workers URL)
            // This URL will be stored in the database and returned to the frontend
            var publicUrl = $"{_settings.PublicBaseUrl.TrimEnd('/')}/{key}";
            Console.WriteLine($"✅ Image uploaded successfully. Public URL: {publicUrl}");
            return publicUrl;
        }

        // WARNING: This URL will NOT work - R2 storage endpoints are not publicly accessible
        // This is only returned as a fallback, but images will not be viewable
        var fallbackUrl = $"https://{_settings.AccountId}.r2.cloudflarestorage.com/{_settings.BucketName}/{key}";
        Console.WriteLine($"⚠️ WARNING: No PublicBaseUrl configured. Images uploaded but not publicly accessible.");
        Console.WriteLine($"⚠️ Generated URL (NOT accessible): {fallbackUrl}");
        Console.WriteLine($"⚠️ To fix: Enable 'Public Development URL' in R2 bucket settings, then set PublicBaseUrl in appsettings.json");
        Console.WriteLine($"⚠️ The Public Development URL will look like: https://pub-xxxxx.r2.dev");
        return fallbackUrl;
    }
}
