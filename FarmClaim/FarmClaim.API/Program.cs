using FarmClaim.API.Middleware;
using FarmClaim.Application.Common.Behaviors;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Infrastructure.Data;
using FarmClaim.Infrastructure.JWT;
using FluentValidation;
using Hangfire;
using Hangfire.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using System.Text;
using FarmClaim.API.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// DATABASE
// ============================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IFileStorageService, FarmClaim.Infrastructure.Services.CloudinaryStorageService>();

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
// AUTHENTICATION (JWT)
// ============================================
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Secret"]
    ?? throw new InvalidOperationException("JWT Secret not configured"));

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
        ClockSkew = TimeSpan.Zero
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
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.FromSeconds(15),
            UseRecommendedIsolationLevel = true
        }));

builder.Services.AddHangfireServer();
builder.Services.AddScoped<FarmClaim.Infrastructure.Jobs.ClaimBackgroundJobService>();
builder.Services.AddScoped<IClaimBackgroundJobService, FarmClaim.Infrastructure.Services.HangfireBackgroundJobService>();
builder.Services.AddScoped<INotificationService, FarmClaim.API.Services.SignalRNotificationService>();

// ============================================
// EMAIL — Production setup (SendGrid + Hangfire + RazorLight)
// ============================================
builder.Services.Configure<FarmClaim.Infrastructure.Configuration.EmailSettings>(
    builder.Configuration.GetSection("Email"));

builder.Services.AddSingleton<FarmClaim.Infrastructure.Email.Services.IEmailTemplateService,
    FarmClaim.Infrastructure.Email.Services.EmailTemplateService>();

builder.Services.AddSingleton<Polly.IAsyncPolicy>(
    FarmClaim.Infrastructure.Email.Policies.EmailRetryPolicy.EmailPolicy);

// Register HttpClient for Elastic Email API
builder.Services.AddHttpClient("ElasticEmail", client =>
{
    client.BaseAddress = new Uri("https://api.elasticemail.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Primary email sender — SmtpEmailService
builder.Services.AddSingleton<FarmClaim.Application.Common.Interfaces.IEmailService,
    FarmClaim.Infrastructure.Services.SmtpEmailService>();

builder.Services.AddSingleton<FarmClaim.Application.Common.Interfaces.IEmailQueueService,
    FarmClaim.Infrastructure.Email.Services.EmailQueueService>();

builder.Services.AddScoped<FarmClaim.Infrastructure.Email.Services.EmailJob>();

// ============================================
// CORS
// ============================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "http://127.0.0.1:3000")
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
// BUILD APP
// ============================================
builder.Services.AddSignalR();
var app = builder.Build();

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

app.UseExceptionHandling();
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new AllowAllDashboardAuthorization() },
    DashboardTitle = "FarmClaim Jobs"
});
app.UseHttpsRedirection();
app.UseRouting();
app.UseCookiePolicy();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

// ============================================
// AUTO-MIGRATE DATABASE ON STARTUP
// ============================================
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
            Console.WriteLine(" Database migrations applied successfully!");
        }
        else
        {
            Console.WriteLine(" Database is up to date.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($" Migration failed: {ex.Message}");
        throw;
    }
}

Console.WriteLine(" FarmClaim API starting...");
await app.RunAsync();

// ============================================
// TYPE DECLARATIONS (must be after all statements)
// ============================================
public class AllowAllDashboardAuthorization : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}