using Hydra.Api.Services.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace Hydra.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IStorageService _storage;

    public UploadController(IStorageService storage)
    {
        _storage = storage;
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<ActionResult<UploadResponse>> Upload(
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
        if (!allowed.Contains(file.ContentType.ToLowerInvariant()))
            return BadRequest(new { message = "Only JPEG, PNG, WebP and GIF images are allowed." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var key = $"uploads/{Guid.NewGuid()}{ext}";

        await using var stream = file.OpenReadStream();
        var url = await _storage.UploadAsync(stream, key, file.ContentType, ct);

        return Ok(new UploadResponse(url));
    }
}

public record UploadResponse(string Url);
