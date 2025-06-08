using Emma.Core.Services;
using Emma.Core.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Diagnostics;

namespace Emma.Api.Middleware;

/// <summary>
/// Development middleware that provides privacy-aware debugging capabilities.
/// Logs request/response data with appropriate masking based on environment.
/// </summary>
public class PrivacyDebugMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PrivacyDebugMiddleware> _logger;
    private readonly IConfiguration _configuration;
    private readonly bool _isEnabled;
    private readonly bool _isProduction;

    public PrivacyDebugMiddleware(
        RequestDelegate next, 
        ILogger<PrivacyDebugMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
        _isEnabled = _configuration.GetValue<bool>("Debug:EnablePrivacyDebugMiddleware", false);
        _isProduction = _configuration.GetValue<bool>("IsProduction", true);
    }

    public async Task InvokeAsync(HttpContext context, IDataMaskingService? maskingService = null)
    {
        if (!_isEnabled || _isProduction)
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var agentId = context.User?.FindFirst("AgentId")?.Value;
        var requestId = context.TraceIdentifier;

        try
        {
            // Log incoming request with masking
            await LogRequestAsync(context, agentId, requestId, maskingService);

            // Capture response
            var originalResponseBody = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            // Log response with masking
            await LogResponseAsync(context, agentId, requestId, responseBody, originalResponseBody, maskingService);

            stopwatch.Stop();
            _logger.LogPerformanceMetric($"Request {context.Request.Method} {context.Request.Path}", 
                stopwatch.Elapsed, agentId: agentId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Privacy Debug Middleware error for request {RequestId} by agent {AgentId}", 
                requestId, agentId);
            throw;
        }
    }

    private async Task LogRequestAsync(HttpContext context, string? agentId, string requestId, 
        IDataMaskingService? maskingService)
    {
        try
        {
            var request = context.Request;
            var requestInfo = new
            {
                RequestId = requestId,
                Method = request.Method,
                Path = request.Path.Value,
                QueryString = request.QueryString.Value,
                Headers = GetSafeHeaders(request.Headers),
                AgentId = agentId,
                IPAddress = context.Connection.RemoteIpAddress?.ToString()
            };

            // Log request body for POST/PUT requests
            if (request.Method == "POST" || request.Method == "PUT")
            {
                request.EnableBuffering();
                var body = await ReadRequestBodyAsync(request);
                
                if (maskingService != null && !string.IsNullOrEmpty(body))
                {
                    var level = maskingService.GetMaskingLevel(agentId, isProduction: false);
                    var maskedBody = maskingService.MaskText(body, level);
                    _logger.LogDebug("Request {RequestId}: {RequestInfo} | Body: {Body}", 
                        requestId, JsonSerializer.Serialize(requestInfo), maskedBody);
                }
                else
                {
                    _logger.LogDebug("Request {RequestId}: {RequestInfo}", 
                        requestId, JsonSerializer.Serialize(requestInfo));
                }
            }
            else
            {
                _logger.LogDebug("Request {RequestId}: {RequestInfo}", 
                    requestId, JsonSerializer.Serialize(requestInfo));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log request for {RequestId}", requestId);
        }
    }

    private async Task LogResponseAsync(HttpContext context, string? agentId, string requestId, 
        MemoryStream responseBody, Stream originalResponseBody, IDataMaskingService? maskingService)
    {
        try
        {
            var response = context.Response;
            responseBody.Seek(0, SeekOrigin.Begin);
            var responseText = await new StreamReader(responseBody).ReadToEndAsync();
            responseBody.Seek(0, SeekOrigin.Begin);

            var responseInfo = new
            {
                RequestId = requestId,
                StatusCode = response.StatusCode,
                ContentType = response.ContentType,
                ContentLength = responseBody.Length
            };

            // Mask response body if it contains sensitive data
            if (maskingService != null && !string.IsNullOrEmpty(responseText) && 
                IsJsonResponse(response.ContentType))
            {
                var level = maskingService.GetMaskingLevel(agentId, isProduction: false);
                var maskedResponse = maskingService.MaskText(responseText, level);
                _logger.LogDebug("Response {RequestId}: {ResponseInfo} | Body: {Body}", 
                    requestId, JsonSerializer.Serialize(responseInfo), maskedResponse);
            }
            else
            {
                _logger.LogDebug("Response {RequestId}: {ResponseInfo}", 
                    requestId, JsonSerializer.Serialize(responseInfo));
            }

            // Copy response back to original stream
            await responseBody.CopyToAsync(originalResponseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log response for {RequestId}", requestId);
        }
    }

    private async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        try
        {
            request.Body.Position = 0;
            using var reader = new StreamReader(request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
            return body;
        }
        catch
        {
            return "[BODY_READ_ERROR]";
        }
    }

    private Dictionary<string, string> GetSafeHeaders(IHeaderDictionary headers)
    {
        var safeHeaders = new Dictionary<string, string>();
        var sensitiveHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization", "Cookie", "X-API-Key", "X-Auth-Token"
        };

        foreach (var header in headers)
        {
            if (sensitiveHeaders.Contains(header.Key))
            {
                safeHeaders[header.Key] = "[MASKED]";
            }
            else
            {
                safeHeaders[header.Key] = header.Value.ToString();
            }
        }

        return safeHeaders;
    }

    private bool IsJsonResponse(string? contentType)
    {
        return contentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true;
    }
}

/// <summary>
/// Extension method to easily add the privacy debug middleware.
/// </summary>
public static class PrivacyDebugMiddlewareExtensions
{
    public static IApplicationBuilder UsePrivacyDebugMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PrivacyDebugMiddleware>();
    }
}
