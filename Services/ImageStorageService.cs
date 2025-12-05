using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
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
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);
}

public class R2ImageStorageService : IImageStorageService
{
    private readonly CloudflareR2Settings _settings;
    private readonly IAmazonS3 _s3;

    public R2ImageStorageService(IOptions<CloudflareR2Settings> options)
    {
        _settings = options.Value;

        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{_settings.AccountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true,
            AuthenticationRegion = "auto"
        };

        var creds = new BasicAWSCredentials(_settings.AccessKeyId, _settings.SecretAccessKey);
        _s3 = new AmazonS3Client(creds, config);
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var key = $"products/{Guid.NewGuid()}-{fileName}";

        var request = new PutObjectRequest
        {
            BucketName = _settings.BucketName,
            Key = key,
            InputStream = stream,
            ContentType = contentType,
            CannedACL = S3CannedACL.PublicRead
        };

        await _s3.PutObjectAsync(request, cancellationToken);

        if (!string.IsNullOrWhiteSpace(_settings.PublicBaseUrl))
        {
            return $"{_settings.PublicBaseUrl.TrimEnd('/')}/{key}";
        }

        // Fallback to direct R2 URL pattern if no CDN URL supplied
        return $"https://{_settings.AccountId}.r2.cloudflarestorage.com/{_settings.BucketName}/{key}";
    }
}
