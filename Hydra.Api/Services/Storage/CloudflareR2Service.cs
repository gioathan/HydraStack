using System.Security.Cryptography;
using System.Text;
using Hydra.Api.Configuration;
using Microsoft.Extensions.Options;

namespace Hydra.Api.Services.Storage;

public class CloudflareR2Service : IStorageService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CloudflareR2Settings _settings;

    public CloudflareR2Service(IHttpClientFactory httpClientFactory, IOptions<CloudflareR2Settings> settings)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
    }

    public async Task<string> UploadAsync(Stream stream, string key, string contentType, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        var body = ms.ToArray();

        var host = $"{_settings.AccountId}.r2.cloudflarestorage.com";
        var path = $"/{_settings.BucketName}/{key}";
        var now = DateTime.UtcNow;
        var dateStamp = now.ToString("yyyyMMdd");
        var amzDate = now.ToString("yyyyMMddTHHmmssZ");
        var payloadHash = Sha256Hex(body);

        var canonicalHeaders = $"content-type:{contentType}\nhost:{host}\nx-amz-content-sha256:{payloadHash}\nx-amz-date:{amzDate}\n";
        const string signedHeaders = "content-type;host;x-amz-content-sha256;x-amz-date";
        var canonicalRequest = $"PUT\n{path}\n\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";

        const string region = "auto";
        var credentialScope = $"{dateStamp}/{region}/s3/aws4_request";
        var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{credentialScope}\n{Sha256Hex(Encoding.UTF8.GetBytes(canonicalRequest))}";

        var signingKey = DeriveSigningKey(_settings.SecretAccessKey, dateStamp, region, "s3");
        var signature = HmacHex(signingKey, stringToSign);

        var authorization = $"AWS4-HMAC-SHA256 Credential={_settings.AccessKeyId}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";

        var url = $"https://{host}{path}";
        var request = new HttpRequestMessage(HttpMethod.Put, url);
        request.Content = new ByteArrayContent(body);
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        request.Headers.TryAddWithoutValidation("Authorization", authorization);
        request.Headers.TryAddWithoutValidation("x-amz-date", amzDate);
        request.Headers.TryAddWithoutValidation("x-amz-content-sha256", payloadHash);

        var client = _httpClientFactory.CreateClient("R2");
        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        return $"{_settings.PublicDomain.TrimEnd('/')}/{key}";
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var host = $"{_settings.AccountId}.r2.cloudflarestorage.com";
        var path = $"/{_settings.BucketName}/{key}";
        var now = DateTime.UtcNow;
        var dateStamp = now.ToString("yyyyMMdd");
        var amzDate = now.ToString("yyyyMMddTHHmmssZ");
        const string payloadHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"; // SHA256("")

        var canonicalHeaders = $"host:{host}\nx-amz-content-sha256:{payloadHash}\nx-amz-date:{amzDate}\n";
        const string signedHeaders = "host;x-amz-content-sha256;x-amz-date";
        var canonicalRequest = $"DELETE\n{path}\n\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";

        const string region = "auto";
        var credentialScope = $"{dateStamp}/{region}/s3/aws4_request";
        var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{credentialScope}\n{Sha256Hex(Encoding.UTF8.GetBytes(canonicalRequest))}";

        var signingKey = DeriveSigningKey(_settings.SecretAccessKey, dateStamp, region, "s3");
        var signature = HmacHex(signingKey, stringToSign);

        var authorization = $"AWS4-HMAC-SHA256 Credential={_settings.AccessKeyId}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";

        var url = $"https://{host}{path}";
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.TryAddWithoutValidation("Authorization", authorization);
        request.Headers.TryAddWithoutValidation("x-amz-date", amzDate);
        request.Headers.TryAddWithoutValidation("x-amz-content-sha256", payloadHash);

        var client = _httpClientFactory.CreateClient("R2");
        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    private static string Sha256Hex(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static byte[] DeriveSigningKey(string secret, string date, string region, string service)
    {
        var kDate = Hmac(Encoding.UTF8.GetBytes("AWS4" + secret), date);
        var kRegion = Hmac(kDate, region);
        var kService = Hmac(kRegion, service);
        return Hmac(kService, "aws4_request");
    }

    private static byte[] Hmac(byte[] key, string data) =>
        HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(data));

    private static string HmacHex(byte[] key, string data) =>
        Convert.ToHexString(Hmac(key, data)).ToLowerInvariant();
}
