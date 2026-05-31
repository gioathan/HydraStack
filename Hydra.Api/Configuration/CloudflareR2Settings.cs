namespace Hydra.Api.Configuration;

public class CloudflareR2Settings
{
    public string AccountId { get; set; } = default!;
    public string AccessKeyId { get; set; } = default!;
    public string SecretAccessKey { get; set; } = default!;
    public string BucketName { get; set; } = default!;
    /// <summary>Public base URL for the R2 bucket, e.g. https://cdn.yourdomain.com</summary>
    public string PublicDomain { get; set; } = default!;
}
