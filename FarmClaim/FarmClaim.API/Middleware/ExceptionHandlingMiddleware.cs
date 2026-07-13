using FarmClaim.Application.Common.Exceptions;
using System.Net;
using System.Text.Json;

namespace FarmClaim.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext ctx)
        {
            try { await _next(ctx); }
            catch (Exception ex) { await HandleAsync(ctx, ex); }
        }

        private async Task HandleAsync(HttpContext ctx, Exception ex)
        {
            _logger.LogError(ex, "Unhandled: {Msg}", ex.Message);
            ctx.Response.ContentType = "application/json";
            var err = new ErrorResponse();

            switch (ex)
            {
                case ValidationException v:
                    ctx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    err.StatusCode = HttpStatusCode.BadRequest;
                    err.Message = "Validation Failed";
                    err.Errors = v.Errors.ToArray();
                    break;
                case NotFoundException n:
                    ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    err.StatusCode = HttpStatusCode.NotFound;
                    err.Message = n.Message;
                    break;
                case FarmClaim.Application.Common.Exceptions.UnauthorizedException u:
                    ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    err.StatusCode = HttpStatusCode.Unauthorized;
                    err.Message = u.Message;
                    break;
                case UnauthorizedAccessException u:
                    ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    err.StatusCode = HttpStatusCode.Unauthorized;
                    err.Message = u.Message;
                    break;
                case InvalidOperationException o:
                    ctx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    err.StatusCode = HttpStatusCode.BadRequest;
                    err.Message = o.Message;
                    break;
                default:
                    ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    err.StatusCode = HttpStatusCode.InternalServerError;
                    err.Message = _env.IsDevelopment() ? ex.Message : "Internal server error";
                    if (_env.IsDevelopment()) err.Details = ex.StackTrace;
                    break;
            }

            await ctx.Response.WriteAsync(JsonSerializer.Serialize(err, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            }));
        }
    }

    public class ErrorResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public string Message { get; set; } = "";
        public string[]? Errors { get; set; }
        public string? Details { get; set; }
    }

    public static class ExceptionHandlingExtensions
    {
        public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
            => builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}