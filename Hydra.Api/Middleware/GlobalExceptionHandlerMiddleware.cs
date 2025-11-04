using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Hydra.Api.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var errorResponse = exception switch
        {
            InvalidOperationException => CreateErrorResponse(
                context,
                HttpStatusCode.BadRequest,
                "Bad Request",
                exception.Message,
                "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            ),

            UnauthorizedAccessException => CreateErrorResponse(
                context,
                HttpStatusCode.Unauthorized,
                "Unauthorized",
                "You are not authorized to access this resource",
                "https://tools.ietf.org/html/rfc7235#section-3.1"
            ),

            KeyNotFoundException => CreateErrorResponse(
                context,
                HttpStatusCode.NotFound,
                "Not Found",
                exception.Message,
                "https://tools.ietf.org/html/rfc7231#section-6.5.4"
            ),

            DbUpdateException dbEx when dbEx.InnerException is PostgresException pgEx => CreateDatabaseErrorResponse(
                context,
                pgEx
            ),

            DbUpdateException => CreateErrorResponse(
                context,
                HttpStatusCode.BadRequest,
                "Database Error",
                "A database error occurred while processing your request",
                "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            ),

            _ => CreateErrorResponse(
                context,
                HttpStatusCode.InternalServerError,
                "Internal Server Error",
                _env.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred. Please try again later.",
                "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            )
        };

        context.Response.StatusCode = errorResponse.Status;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _env.IsDevelopment()
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, options));
    }

    private ErrorResponse CreateErrorResponse(
        HttpContext context,
        HttpStatusCode statusCode,
        string title,
        string detail,
        string type)
    {
        return new ErrorResponse
        {
            Type = type,
            Title = title,
            Status = (int)statusCode,
            Detail = detail,
            Instance = context.Request.Path,
            TraceId = context.TraceIdentifier
        };
    }

    private ErrorResponse CreateDatabaseErrorResponse(HttpContext context, PostgresException pgEx)
    {
        var (statusCode, title, detail) = pgEx.SqlState switch
        {
            "23503" => (
                HttpStatusCode.BadRequest,
                "Foreign Key Violation",
                ExtractForeignKeyError(pgEx)
            ),
            "23505" => (
                HttpStatusCode.Conflict,
                "Duplicate Entry",
                "A record with this value already exists"
            ),
            "23502" => (
                HttpStatusCode.BadRequest,
                "Required Field Missing",
                "A required field is missing or null"
            ),
            "23514" => (
                HttpStatusCode.BadRequest,
                "Check Constraint Violation",
                "The provided value violates a constraint"
            ),
            _ => (
                HttpStatusCode.BadRequest,
                "Database Error",
                _env.IsDevelopment()
                    ? $"Database error: {pgEx.MessageText}"
                    : "A database error occurred"
            )
        };

        return new ErrorResponse
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = title,
            Status = (int)statusCode,
            Detail = detail,
            Instance = context.Request.Path,
            TraceId = context.TraceIdentifier
        };
    }

    private string ExtractForeignKeyError(PostgresException pgEx)
    {
        if (_env.IsDevelopment())
        {
            return $"Foreign key constraint '{pgEx.ConstraintName}' violated on table '{pgEx.TableName}'";
        }

        var constraintName = pgEx.ConstraintName ?? "unknown";

        if (constraintName.Contains("VenueType"))
            return "The specified venue type does not exist";
        if (constraintName.Contains("Venue"))
            return "The specified venue does not exist";
        if (constraintName.Contains("Customer"))
            return "The specified customer does not exist";
        if (constraintName.Contains("User"))
            return "The specified user does not exist";

        return "A related record does not exist";
    }
}