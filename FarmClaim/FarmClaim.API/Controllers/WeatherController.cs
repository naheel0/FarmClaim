using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;

namespace FarmClaim.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public class WeatherController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WeatherController> _logger;

        public WeatherController(IHttpClientFactory httpClientFactory, ILogger<WeatherController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentWeather(
            [FromQuery] double lat,
            [FromQuery] double lon)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri("https://api.open-meteo.com");

                var url = $"/v1/forecast?latitude={lat}&longitude={lon}" +
                          "&current=temperature_2m,relative_humidity_2m,wind_speed_10m,weather_code,precipitation" +
                          "&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum,wind_speed_10m_max" +
                          "&forecast_days=1&timezone=auto";

                _logger.LogInformation("Fetching current weather for {Lat},{Lon}", lat, lon);

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadFromJsonAsync<JsonElement>();

                var current = json.GetProperty("current");
                var daily = json.GetProperty("daily");

                var temperature = current.GetProperty("temperature_2m").GetDouble();
                var humidity = current.GetProperty("relative_humidity_2m").GetDouble();
                var windSpeed = current.GetProperty("wind_speed_10m").GetDouble();
                var weatherCode = current.GetProperty("weather_code").GetInt32();
                var precipitation = current.GetProperty("precipitation").GetDouble();

                var maxTemp = daily.GetProperty("temperature_2m_max")[0].GetDouble();
                var minTemp = daily.GetProperty("temperature_2m_min")[0].GetDouble();
                var dailyRainfall = daily.GetProperty("precipitation_sum")[0].GetDouble();
                var maxWind = daily.GetProperty("wind_speed_10m_max")[0].GetDouble();

                var result = new
                {
                    temperatureCelsius = temperature,
                    feelsLikeCelsius = temperature,
                    humidity = humidity,
                    windSpeedKmh = windSpeed,
                    precipitation = precipitation,
                    weatherCondition = MapWeatherCode(weatherCode),
                    weatherCode = weatherCode,
                    dailyMaxTemp = maxTemp,
                    dailyMinTemp = minTemp,
                    dailyRainfall = dailyRainfall,
                    dailyMaxWind = maxWind,
                    date = DateOnly.FromDateTime(DateTime.UtcNow),
                    source = "Open-Meteo"
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch weather for {Lat},{Lon}", lat, lon);
                return StatusCode(502, new { error = "Weather service unavailable", message = ex.Message });
            }
        }

        private static string MapWeatherCode(int code) => code switch
        {
            0 => "Clear sky",
            1 => "Mainly clear",
            2 => "Partly cloudy",
            3 => "Overcast",
            45 or 48 => "Fog",
            51 or 53 or 55 => "Drizzle",
            56 or 57 => "Freezing drizzle",
            61 or 63 or 65 => "Rain",
            66 or 67 => "Freezing rain",
            71 or 73 or 75 or 77 => "Snowfall",
            80 or 81 or 82 => "Rain showers",
            85 or 86 => "Snow showers",
            95 => "Thunderstorm",
            96 or 99 => "Thunderstorm with hail",
            _ => "Unknown"
        };
    }
}
