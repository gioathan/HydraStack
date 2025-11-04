namespace Hydra.Api.Middleware;

public class ErrorResponse
{
    public string Type { get; set; } = default!;
    public string Title { get; set; } = default!;
    public int Status { get; set; }
    public string? Detail { get; set; }
    public string? Instance { get; set; }
    public string TraceId { get; set; } = default!;
    public Dictionary<string, string[]>? Errors { get; set; }
}