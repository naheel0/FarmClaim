using FarmClaim.API.Hubs;
using FarmClaim.API.Middleware;
using FarmClaim.Application.Common.Behaviors;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Common.Services;
using FarmClaim.Infrastructure.Data;
using FarmClaim.Infrastructure.JWT;
using FarmClaim.Infrastructure.Services;
using FluentValidation;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// DATABASE (with Audit Interceptor)
// ============================================
builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

    // Register the audit interceptor (Task #7)
    var auditInterceptor = sp.GetRequiredService<ISaveChangesInterceptor>();
    options.AddInterceptors(auditInterceptor);
});

builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IPolicyCreationService, PolicyCreationService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IFileStorageService, FarmClaim.Infrastructure.Services.CloudinaryStorageService>();

// ============================================
// AUDIT LOGGING (Task #7)
// ============================================
builder.Services.AddScoped<IAuditService, FarmClaim.Infrastructure.Data.Audit.AuditService>();
builder.Services.AddScoped<ISaveChangesInterceptor, FarmClaim.Infrastructure.Data.Audit.AuditSaveChangesInterceptor>();

// ============================================
// RATE LIMITING REGISTRATION (Task #5)
// ============================================
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<RateLimitingPolicy>();

// ============================================
// EXTERNAL API SERVICES (with Polly resilience)
// ============================================
builder.Services.AddHttpClient<IWeatherService, FarmClaim.Infrastructure.Services.WeatherApiService>()
    .AddPolicyHandler(GetRetryPolicy("Weather API", 3));

builder.Services.AddHttpClient<IGeminiVisionService, FarmClaim.Infrastructure.Services.GeminiVisionService>(client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com");
})
.AddPolicyHandler(GetRetryPolicy("Gemini Vision API", 3));

// H11 FIX: Separate HttpClient for downloading images (no API key attached)
// Set a 15s timeout to prevent slow downloads from consuming Hangfire job lease time
builder.Services.AddHttpClient("GeminiDownload", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});

// ============================================
// MEDIATR (CQRS)
// ============================================
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
});

// ============================================
// FLUENTVALIDATION REGISTRATION
// ============================================
builder.Services.AddValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());

// ============================================
// PIPELINE BEHAVIORS
// ============================================
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

// ============================================
// COOKIE POLICY
// ============================================
if (builder.Environment.IsDevelopment())
{
    builder.Services.Configure<CookiePolicyOptions>(options =>
    {
        options.MinimumSameSitePolicy = SameSiteMode.Lax;
        options.Secure = CookieSecurePolicy.None;
        options.HttpOnly = HttpOnlyPolicy.Always;
    });
}
else
{
    builder.Services.Configure<CookiePolicyOptions>(options =>
    {
        options.MinimumSameSitePolicy = SameSiteMode.Strict;
        options.Secure = CookieSecurePolicy.Always;
        options.HttpOnly = HttpOnlyPolicy.Always;
    });
}

// ============================================
// AUTHENTICATION (JWT) - reads secret from env var OR config
// ============================================
var jwtSettings = builder.Configuration.GetSection("Jwt");

// H17 FIX: JWT secret from env var (production-safe)
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
                ?? jwtSettings["Secret"]
                ?? throw new InvalidOperationException("JWT Secret not configured");

// H17 FIX: Reject placeholder or short secrets — anyone who knows the default can forge admin tokens
if (jwtSecret.Length < 32 || jwtSecret.Contains("your-super-secret", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        "JWT Secret is unsafe (too short or contains placeholder text). " +
        "Set a strong secret via JWT_SECRET environment variable (min 32 characters).");
}

var secretKey = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ClockSkew = TimeSpan.FromSeconds(30)
    };

    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;

            var error = new
            {
                statusCode = 401,
                error = "Unauthorized",
                message = "Invalid or expired token. Please login again.",
                traceId = context.HttpContext.TraceIdentifier,
                path = context.HttpContext.Request.Path
            };

            await context.Response.WriteAsync(
                System.Text.Json.JsonSerializer.Serialize(error,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                        WriteIndented = true
                    }));
        },
        OnForbidden = async context =>
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status403Forbidden;

            var error = new
            {
                statusCode = 403,
                error = "Forbidden",
                message = "You do not have permission to access this resource.",
                traceId = context.HttpContext.TraceIdentifier,
                path = context.HttpContext.Request.Path
            };

            await context.Response.WriteAsync(
                System.Text.Json.JsonSerializer.Serialize(error,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                        WriteIndented = true
                    }));
        }
    };
});

builder.Services.AddAuthorization();

// ============================================
// HANGFIRE BACKGROUND JOBS
// ============================================
builder.Services.AddHangfire(config => config
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new Hangfire.SqlServer.SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(10),   // H5: increase for long Gemini jobs
            QueuePollInterval = TimeSpan.FromSeconds(15),
            UseRecommendedIsolationLevel = true,
            // SchemaName = "hangfire" — requires CREATE SCHEMA permission on Azure SQL
            // Reverted: Azure Container Apps user doesn't have permission to create schemas
        }));

builder.Services.AddHangfireServer();
builder.Services.AddScoped<FarmClaim.Infrastructure.Jobs.ClaimBackgroundJobService>();
builder.Services.AddScoped<IClaimBackgroundJobService, FarmClaim.Infrastructure.Services.HangfireBackgroundJobService>();
builder.Services.AddScoped<INotificationService, FarmClaim.API.Services.SignalRNotificationService>();
builder.Services.AddScoped<FarmClaim.Infrastructure.Jobs.MaintenanceJobs>();

// ============================================
// EMAIL — Production setup (Hangfire + RazorLight)
// ============================================
builder.Services.Configure<FarmClaim.Infrastructure.Configuration.EmailSettings>(
    builder.Configuration.GetSection("Email"));

builder.Services.AddSingleton<FarmClaim.Infrastructure.Email.Services.IEmailTemplateService,
    FarmClaim.Infrastructure.Email.Services.EmailTemplateService>();

builder.Services.AddSingleton<Polly.IAsyncPolicy>(
    FarmClaim.Infrastructure.Email.Policies.EmailRetryPolicy.EmailPolicy);

builder.Services.AddHttpClient("ElasticEmail", client =>
{
    client.BaseAddress = new Uri("https://api.elasticemail.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<FarmClaim.Application.Common.Interfaces.IEmailService,
    FarmClaim.Infrastructure.Email.Services.ElasticEmailService>();

builder.Services.AddScoped<FarmClaim.Application.Common.Interfaces.IEmailQueueService,
    FarmClaim.Infrastructure.Email.Services.EmailQueueService>();

builder.Services.AddScoped<FarmClaim.Infrastructure.Email.Services.EmailJob>();

// ============================================
// RAZORPAY PAYMENT (Task #2)
// ============================================
builder.Services.Configure<FarmClaim.Infrastructure.Configuration.RazorpaySettings>(
    builder.Configuration.GetSection("Razorpay"));

builder.Services.AddScoped<FarmClaim.Application.Common.Interfaces.IPaymentService,
    FarmClaim.Infrastructure.Services.RazorpayPaymentService>();

// ============================================
// CORS — Configurable via appsettings (Fix #7)
// ============================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000", "http://localhost:5173", "http://127.0.0.1:3000" };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ============================================
// CONTROLLERS & JSON OPTIONS
// ============================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, FarmClaim.API.Services.UserIdProvider>();

// ============================================
// SWAGGER
// ============================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FarmClaim API",
        Version = "v1",
        Description = "AI-powered Crop Insurance Claim Management System"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ============================================
// POLLY RETRY POLICY (local function)
// ============================================
IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(string serviceName, int maxRetries)
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: maxRetries,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Console.WriteLine($"[Polly] {serviceName} retry {retryCount}/{maxRetries} after {timespan.TotalSeconds}s");
            });
}

// ============================================
// RATE LIMITING CONFIG (Task #5)
// ============================================
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy<string, RateLimitingPolicy>("FarmClaimPolicy");
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            $"global:{httpContext.Request.Path}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 1000,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

// ============================================
// HEALTH CHECKS (Task #6)
// ============================================
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(
        name: "Database",
        tags: new[] { "db", "sql", "core" })
    .AddHangfire(
        setup: options =>
        {
            // Options can be left empty — uses defaults
        },
        name: "Hangfire",
        tags: new[] { "jobs", "background" })
    .AddCheck("Self", () => HealthCheckResult.Healthy("API is running"), tags: new[] { "self" });

// ============================================
// BUILD APP
// ============================================
builder.Services.AddSignalR();
var app = builder.Build();

// ============================================
// STARTUP VERIFICATION
// ============================================
var emailServiceType = app.Services.GetRequiredService<FarmClaim.Application.Common.Interfaces.IEmailService>().GetType().Name;
var elasticApiKey = Environment.GetEnvironmentVariable("ELASTIC_EMAIL_API_KEY");
var emailSettings = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<FarmClaim.Infrastructure.Configuration.EmailSettings>>().Value;
Console.WriteLine($"[STARTUP] IEmailService => {emailServiceType}");
Console.WriteLine($"[STARTUP] ELASTIC_EMAIL_API_KEY => {(string.IsNullOrEmpty(elasticApiKey) ? "NOT SET" : "SET (length=" + elasticApiKey.Length + ")")}");
Console.WriteLine($"[STARTUP] DummyMode => {emailSettings.DummyMode}");

// M13 FIX: Fail startup in production if API key is missing — app starts but emails silently fail
if (!emailSettings.DummyMode && app.Environment.IsProduction()
    && (string.IsNullOrEmpty(elasticApiKey) || elasticApiKey == "your-elastic-email-api-key-here"))
{
    throw new InvalidOperationException(
        "ElasticEmail API key is not configured in Production. " +
        "Set the ELASTIC_EMAIL_API_KEY environment variable.");
}

// ============================================
// HTTP PIPELINE
// ============================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    // FIX #7: HSTS for production
    app.UseHsts();
}

app.UseExceptionHandling();
app.UseHttpsRedirection();
app.UseRouting();
app.UseCookiePolicy();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

// FIX: Hangfire dashboard AFTER UseAuthorization (requires auth to work)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new AdminOnlyHangfireAuthorization() },
    DashboardTitle = "FarmClaim Jobs"
});
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

// ============================================
// DATABASE MIGRATIONS
// In production, use CI/CD: dotnet ef database update --project FarmClaim.Infrastructure
// In development, auto-apply pending migrations for convenience.
// ============================================
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                await db.Database.MigrateAsync();
                Console.WriteLine("Database migrations applied successfully.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Migration failed: {ex.Message}");
            throw;
        }
    }
}

// ============================================
// SCHEDULE RECURRING JOBS
// Hangfire resolves services from its own DI scope at execution time.
// M1 FIX: Use explicit IST timezone — container runs UTC, but jobs should fire at IST times.
// ============================================
var ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

RecurringJob.AddOrUpdate<FarmClaim.Infrastructure.Jobs.MaintenanceJobs>(
    "expire-policies-daily",
    job => job.ExpirePoliciesAsync(),
    Cron.Daily(1, 0), ist);

RecurringJob.AddOrUpdate<FarmClaim.Infrastructure.Jobs.MaintenanceJobs>(
    "cleanup-tokens-daily",
    job => job.CleanupExpiredTokensAsync(),
    Cron.Daily(2, 0), ist);

RecurringJob.AddOrUpdate<FarmClaim.Infrastructure.Jobs.MaintenanceJobs>(
    "policy-expiry-reminder-daily",
    job => job.SendPolicyExpiryRemindersAsync(),
    Cron.Daily(9, 0), ist);

RecurringJob.AddOrUpdate<FarmClaim.Infrastructure.Jobs.MaintenanceJobs>(
    "cancel-stale-policies-weekly",
    job => job.CancelStalePendingPoliciesAsync(),
    Cron.Weekly(DayOfWeek.Sunday, 3, 0), ist);

RecurringJob.AddOrUpdate<FarmClaim.Infrastructure.Jobs.MaintenanceJobs>(
    "cancel-overdue-installments-daily",
    job => job.CancelOverdueInstallmentPoliciesAsync(),
    Cron.Daily(4, 0), ist);

Console.WriteLine("Recurring jobs scheduled: expire-policies, cleanup-tokens, expiry-reminder, cancel-stale-policies");

Console.WriteLine("FarmClaim API starting...");
await app.RunAsync();