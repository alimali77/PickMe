using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PickMe.Domain.Common;

namespace PickMe.Api.Common;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;

    public async Task Invoke(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain exception: {Code}", ex.Code);
            await WriteAsync(ctx, StatusCodes.Status409Conflict, ex.Code, ex.Message);
        }
        catch (OperationCanceledException) when (ctx.RequestAborted.IsCancellationRequested)
        {
            // client aborted, no-op
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteAsync(ctx, StatusCodes.Status500InternalServerError, "internal_error", "Beklenmeyen bir hata oluştu.");
        }
    }

    private static async Task WriteAsync(HttpContext ctx, int status, string code, string message)
    {
        if (ctx.Response.HasStarted) return;
        ctx.Response.Clear();
        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode = status;
        var payload = ApiResponse<object>.Fail(code, message);
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        }));
    }
}
