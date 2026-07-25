using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;
using FarmClaim.Infrastructure.Data;

namespace FarmClaim.API.Controllers
{
    /// <summary>
    /// Health check endpoints for monitoring tools, Docker/K8s probes, and uptime checks.
    /// All endpoints are anonymous (no auth required).
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [AllowAnonymous]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly HealthCheckService _healthCheckService;
        private readonly ILogger<HealthController> _logger;
        private static readonly DateTime _startTime = DateTime.UtcNow;

        public HealthController(
            ApplicationDbContext dbContext,
            HealthCheckService healthCheckService,
            ILogger<HealthController> logger)
        {
            _dbContext = dbContext;
            _healthCheckService = healthCheckService;
            _logger = logger;
        }

        // ============================================
        // LIVENESS PROBE — Is the app running?
        // GET /api/v1/health
        // Use for: Kubernetes livenessProbe, Docker HEALTHCHECK, uptime monitors
        // ============================================
        [HttpGet]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public IActionResult GetLiveness()
        {
            return Ok(new
            {
                status = "Healthy",
                app = "FarmClaim API",
                version = "1.0.0",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                timestamp = DateTime.UtcNow,
                uptimeSeconds = (int)(DateTime.UtcNow - _startTime).TotalSeconds,
                machineName = Environment.MachineName,
                processId = Environment.ProcessId
            });
        }

        // ============================================
        // READINESS PROBE — Are all dependencies OK?
        // GET /api/v1/health/detail
        // Use for: Kubernetes readinessProbe, deployment gating, deep monitoring
        // ============================================
        [HttpGet("detail")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> GetReadiness()
        {
            var report = await _healthCheckService.CheckHealthAsync();

            var response = new
            {
                status = report.Status.ToString(),
                timestamp = DateTime.UtcNow,
                totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    durationMs = Math.Round(e.Value.Duration.TotalMilliseconds, 2),
                    tags = e.Value.Tags,
                    data = e.Value.Data.Count > 0
                        ? e.Value.Data.Select(d => new { key = d.Key, value = d.Value?.ToString() })
                        : null
                })
            };

            if (report.Status == HealthStatus.Healthy)
            {
                return Ok(response);
            }

            _logger.LogWarning("Health check degraded. Status: {Status}", report.Status);
            return StatusCode(503, response);
        }

        // ============================================
        // DB PING — Quick database connectivity check
        // GET /api/v1/health/db
        // Use for: Quick DB-only monitoring, alerting
        // ============================================
        [HttpGet("db")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> GetDbHealth()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var canConnect = await _dbContext.Database.CanConnectAsync();
                sw.Stop();

                if (!canConnect)
                {
                    _logger.LogError("DB health check failed: Cannot connect to database");
                    return StatusCode(503, new
                    {
                        status = "Unhealthy",
                        dependency = "Database",
                        message = "Cannot connect to database",
                        durationMs = Math.Round(sw.Elapsed.TotalMilliseconds, 2)
                    });
                }

                return Ok(new
                {
                    status = "Healthy",
                    dependency = "Database",
                    database = _dbContext.Database.GetDbConnection().Database,
                    durationMs = Math.Round(sw.Elapsed.TotalMilliseconds, 2)
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "DB health check threw exception");
                return StatusCode(503, new
                {
                    status = "Unhealthy",
                    dependency = "Database",
                    error = ex.Message,
                    durationMs = Math.Round(sw.Elapsed.TotalMilliseconds, 2)
                });
            }
        }
    }
}