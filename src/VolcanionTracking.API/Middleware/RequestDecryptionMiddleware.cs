using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using VolcanionTracking.Application.Common.Interfaces;

namespace VolcanionTracking.API.Middleware;

/// <summary>
/// Middleware to decrypt and verify encrypted requests
/// </summary>
public class RequestDecryptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestDecryptionMiddleware> _logger;

    public RequestDecryptionMiddleware(
        RequestDelegate next,
        ILogger<RequestDecryptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IRequestDecryptionService decryptionService)
    {
        // Only process POST/PUT/PATCH requests
        if (context.Request.Method != HttpMethods.Post &&
            context.Request.Method != HttpMethods.Put &&
            context.Request.Method != HttpMethods.Patch)
        {
            await _next(context);
            return;
        }

        // Skip health check and metrics endpoints
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/metrics") ||
            context.Request.Path.StartsWithSegments("/scalar") ||
            context.Request.Path.StartsWithSegments("/openapi"))
        {
            await _next(context);
            return;
        }

        // Read the request body
        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var requestBody = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        // Try to parse as encrypted request
        EncryptedRequestDto? encryptedRequest;
        try
        {
            encryptedRequest = JsonSerializer.Deserialize<EncryptedRequestDto>(
                requestBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse request as encrypted request");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Invalid request format",
                message = "Request must be in encrypted format"
            });
            return;
        }

        if (encryptedRequest == null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Invalid request",
                message = "Request body is required"
            });
            return;
        }

        // Decrypt and verify
        var (isValid, decryptedData, error) = await decryptionService.DecryptAndVerifyAsync(
            encryptedRequest.Data,
            encryptedRequest.RequestId,
            encryptedRequest.RequestTime,
            encryptedRequest.Partner,
            encryptedRequest.Sign,
            context.RequestAborted);

        if (!isValid)
        {
            _logger.LogWarning(
                "Request decryption/verification failed: {Error}, Partner: {Partner}, RequestId: {RequestId}",
                error,
                encryptedRequest.Partner,
                encryptedRequest.RequestId);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Request verification failed",
                message = error,
                requestId = encryptedRequest.RequestId
            });
            return;
        }

        _logger.LogInformation(
            "Request decrypted successfully for Partner: {Partner}, RequestId: {RequestId}",
            encryptedRequest.Partner,
            encryptedRequest.RequestId);

        // Store decrypted data and partner info in HttpContext items
        context.Items["DecryptedData"] = decryptedData;
        context.Items["PartnerCode"] = encryptedRequest.Partner;
        context.Items["RequestId"] = encryptedRequest.RequestId;
        context.Items["RequestTime"] = encryptedRequest.RequestTime;

        // Replace request body with decrypted data
        var decryptedBytes = System.Text.Encoding.UTF8.GetBytes(decryptedData!);
        context.Request.Body = new MemoryStream(decryptedBytes);
        context.Request.ContentLength = decryptedBytes.Length;

        await _next(context);
    }
}

public class EncryptedRequestDto
{
    public string Data { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string RequestTime { get; set; } = string.Empty;
    public string Partner { get; set; } = string.Empty;
    public string Sign { get; set; } = string.Empty;
}
