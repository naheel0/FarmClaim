using FarmClaim.Application.Common.Exceptions;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace FarmClaim.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await HandleExceptionAsync(context, ex, stopwatch.ElapsedMilliseconds);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex, long elapsedMs)
        {
            // H3 FIX: If response already started streaming, we can't write to it.
            // Attempting to set StatusCode throws InvalidOperationException which hides the original error.
            if (context.Response.HasStarted)
            {
                _logger.LogError(ex, "Response already started; cannot write error response. Rethrowing.");
                return;
            }

            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
            var response = context.Response;
            response.ContentType = "application/json";

            var errorResponse = new ErrorResponse
            {
                TraceId = traceId,
                Timestamp = DateTime.UtcNow
            };

            switch (ex)
            {
                case ValidationException v:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Error = "Validation Failed";
                    errorResponse.Message = "One or more validation errors occurred.";
                    errorResponse.Errors = v.Errors.ToArray();
                    if (v.PropertyErrors.Count > 0)
                    {
                        errorResponse.FieldErrors = v.PropertyErrors
                            .ToDictionary(kv => kv.Key, kv => kv.Value);
                    }
                    _logger.LogWarning("Validation failed [{TraceId}]: {Errors}", traceId, string.Join(", ", v.Errors));
                    break;

                case NotFoundException n:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.Error = "Not Found";
                    errorResponse.Message = n.Message;
                    _logger.LogWarning("Not found [{TraceId}]: {Message}", traceId, n.Message);
                    break;

                case UnauthorizedAccessException u:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.Error = "Unauthorized";
                    errorResponse.Message = u.Message;
                    _logger.LogWarning("Unauthorized [{TraceId}]: {Message}", traceId, u.Message);
                    break;

                case ForbiddenException f:
                    response.StatusCode = (int)HttpStatusCode.Forbidden;
                    errorResponse.StatusCode = (int)HttpStatusCode.Forbidden;
                    errorResponse.Error = "Forbidden";
                    errorResponse.Message = f.Message;
                    _logger.LogWarning("Forbidden [{TraceId}]: {Message}", traceId, f.Message);
                    break;

                case ConflictException c:
                    response.StatusCode = (int)HttpStatusCode.Conflict;
                    errorResponse.StatusCode = (int)HttpStatusCode.Conflict;
                    errorResponse.Error = "Conflict";
                    errorResponse.Message = c.Message;
                    _logger.LogWarning("Conflict [{TraceId}]: {Message}", traceId, c.Message);
                    break;

                case InvalidOperationException o:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Error = "Bad Request";
                    errorResponse.Message = o.Message;
                    _logger.LogWarning("Invalid operation [{TraceId}]: {Message}", traceId, o.Message);
                    break;

                case TimeoutException t:
                    response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    errorResponse.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    errorResponse.Error = "Request Timeout";
                    errorResponse.Message = "The request timed out. Please try again.";
                    _logger.LogError(t, "Timeout [{TraceId}]: {Message}", traceId, t.Message);
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.Error = "Internal Server Error";
                    errorResponse.Message = _env.IsDevelopment()
                        ? ex.Message
                        : "An unexpected error occurred. Please try again later.";
                    _logger.LogError(ex,
                        "Unhandled exception [{TraceId}] {Type}: {Message}\n{StackTrace}",
                        traceId, ex.GetType().Name, ex.Message, ex.StackTrace);
                    break;
            }

            errorResponse.Path = context.Request.Path;
            errorResponse.DurationMs = elapsedMs;

            if (_env.IsDevelopment())
            {
                errorResponse.ExceptionType = ex.GetType().FullName;
                errorResponse.StackTrace = ex.StackTrace;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _env.IsDevelopment()
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, options));
        }
    }

    // ============================================
    // STANDARDIZED ERROR RESPONSE
    // ============================================
    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Error { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string TraceId { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public long DurationMs { get; set; }
        public string[]? Errors { get; set; }
        public Dictionary<string, string[]>? FieldErrors { get; set; }
        public string? ExceptionType { get; set; }
        public string? StackTrace { get; set; }
    }

    // ============================================
    // EXTENSION METHOD
    // ============================================
    public static class ExceptionHandlingExtensions
    {
        public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}