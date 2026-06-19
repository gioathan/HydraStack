using Hydra.Api.Configuration;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace Hydra.Api.Services.Storage;

public class CloudflareR2Service : IStorageService
{
    private readonly IMinioClient _minio;
    private readonly CloudflareR2Settings _settings;

    public CloudflareR2Service(IMinioClient minio, IOptions<CloudflareR2Settings> settings)
    {
        _minio = minio;
        _settings = settings.Value;
    }

    public async Task<string> UploadAsync(Stream stream, string key, string contentType, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        ms.Position = 0;

        var args = new PutObjectArgs()
            .WithBucket(_settings.BucketName)
            .WithObject(key)
            .WithStreamData(ms)
            .WithObjectSize(ms.Length)
            .WithContentType(contentType);

        await _minio.PutObjectAsync(args, ct);
        return $"{_settings.PublicDomain.TrimEnd('/')}/{key}";
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var args = new RemoveObjectArgs()
            .WithBucket(_settings.BucketName)
            .WithObject(key);

        await _minio.RemoveObjectAsync(args, ct);
    }
}
