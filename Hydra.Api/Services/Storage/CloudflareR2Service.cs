using Amazon.S3;
using Amazon.S3.Model;
using Hydra.Api.Configuration;
using Microsoft.Extensions.Options;

namespace Hydra.Api.Services.Storage;

public class CloudflareR2Service : IStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly CloudflareR2Settings _settings;

    public CloudflareR2Service(IAmazonS3 s3, IOptions<CloudflareR2Settings> settings)
    {
        _s3 = s3;
        _settings = settings.Value;
    }

    public async Task<string> UploadAsync(Stream stream, string key, string contentType, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _settings.BucketName,
            Key = key,
            InputStream = stream,
            ContentType = contentType,
            DisablePayloadSigning = true
        };

        await _s3.PutObjectAsync(request, ct);
        return $"{_settings.PublicDomain.TrimEnd('/')}/{key}";
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        await _s3.DeleteObjectAsync(_settings.BucketName, key, ct);
    }
}
