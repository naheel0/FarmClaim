using FarmClaim.API.Middleware;
using FarmClaim.Application.Common.Behaviors;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Infrastructure.Data;
using FarmClaim.Infrastructure.JWT;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.CookiePolicy;
// ✅ REQUIRED for cookies
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

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

// ============================================
// MEDIATR (CQRS)
// ============================================
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
});

// ============================================
// ✅ FLUENTVALIDATION REGISTRATION (CORRECT WAY)
// ============================================

// Method A: AddValidatorsFromAssemblies (requires both usings above)
builder.Services.AddValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());

// If Method A doesn't work, uncomment Method B instead:
// builder.Services.AddScoped(typeof(IValidator<>), typeof(AbstractValidator<>));

// ============================================
// PIPELINE BEHAVIORS
// ============================================
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

// ============================================
// ✅ COOKIE POLICY - FIXED ENUM NAMES
// ============================================
if (builder.Environment.IsDevelopment())
{
    // Development: Allow HTTP (for localhost testing)
    builder.Services.Configure<CookiePolicyOptions>(options =>
    {
        options.MinimumSameSitePolicy = SameSiteMode.Lax;
        // ✅ FIX: CookieSecureOption → CookieSecurePolicy
        options.Secure = CookieSecurePolicy.None;
        options.HttpOnly = HttpOnlyPolicy.Always;
    });
}
else
{
    // Production: Enforce HTTPS only
    builder.Services.Configure<CookiePolicyOptions>(options =>
    {
        options.MinimumSameSitePolicy = SameSiteMode.Strict;
        // ✅ FIX: Correct enum name
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
});

builder.Services.AddAuthorization();

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
// BUILD APP
// ============================================
var app = builder.Build();

// ============================================
// HTTP PIPELINE (ORDER MATTERS!)
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

// Middleware pipeline order:
app.UseExceptionHandling();     // 1st: Error handling
app.UseHttpsRedirection();       // 2nd: HTTPS redirect
app.UseRouting();               // 3rd: Routing
app.UseCookiePolicy();           // 4th: ✅ Cookie policy (MUST be before auth!)
app.UseCors("AllowFrontend");   // 5th: CORS
app.UseAuthentication();         // 6th: Authentication
app.UseAuthorization();          // 7th: Authorization
app.MapControllers();            // 8th: Endpoints

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
            Console.WriteLine("✅ Database migrations applied successfully!");
        }
        else
        {
            Console.WriteLine("✅ Database is up to date.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Migration failed: {ex.Message}");
        throw;
    }
}

Console.WriteLine("🚀 FarmClaim API starting...");
await app.RunAsync();