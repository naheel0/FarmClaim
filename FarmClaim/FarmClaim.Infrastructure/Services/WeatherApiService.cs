using FarmClaim.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace FarmClaim.Infrastructure.Services
{
    public class WeatherApiService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WeatherApiService> _logger;
        private readonly string _provider;

        public WeatherApiService(HttpClient httpClient, IConfiguration config, ILogger<WeatherApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _provider = config["WeatherApi:Provider"] ?? "OpenMeteo";
            _httpClient.BaseAddress = new Uri("https://archive-api.open-meteo.com");
        }

        public async Task<WeatherSnapshot> GetWeatherAsync(double latitude, double longitude, DateTime date, CancellationToken ct = default)
        {
            _logger.LogInformation("Fetching weather for {Lat},{Lon} on {Date}", latitude, longitude, date.ToString("yyyy-MM-dd"));

            var url = $"/v1/archive?latitude={latitude}&longitude={longitude}" +
                      $"&start_date={date:yyyy-MM-dd}&end_date={date:yyyy-MM-dd}" +
                      "&hourly=temperature_2m,precipitation,windspeed_10m,weathercode" +
                      "&timezone=auto";

            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

            var hourly = json.GetProperty("hourly");
            var times = hourly.GetProperty("time");
            var temps = hourly.GetProperty("temperature_2m");
            var rainfalls = hourly.GetProperty("precipitation");
            var winds = hourly.GetProperty("windspeed_10m");
            var codes = hourly.GetProperty("weathercode");

            double maxTemp = double.MinValue;
            double totalRain = 0;
            double maxWind = double.MinValue;
            int dominantCode = 0;
            var codeCount = new Dictionary<int, int>();

            for (int i = 0; i < times.GetArrayLength(); i++)
            {
                if (!temps[i].TryGetDouble(out var temp)) continue;
                if (temp > maxTemp) maxTemp = temp;

                if (rainfalls[i].TryGetDouble(out var rain)) totalRain += rain;
                if (winds[i].TryGetDouble(out var wind) && wind > maxWind) maxWind = wind;

                if (codes[i].TryGetInt32(out var code))
                {
                    codeCount[code] = codeCount.GetValueOrDefault(code, 0) + 1;
                }
            }

            if (codeCount.Count > 0)
                dominantCode = codeCount.OrderByDescending(x => x.Value).First().Key;

            var snapshot = new WeatherSnapshot
            {
                Date = date,
                TemperatureCelsius = Math.Round(maxTemp, 1),
                RainfallMm = Math.Round(totalRain, 1),
                WindSpeedKmh = Math.Round(maxWind, 1),
                WeatherCondition = MapWeatherCode(dominantCode),
                Source = _provider
            };

            _logger.LogInformation("Weather fetched: {Temp}C, {Rain}mm rain, {Wind}km/h wind, {Condition}",
                snapshot.TemperatureCelsius, snapshot.RainfallMm, snapshot.WindSpeedKmh, snapshot.WeatherCondition);

            return snapshot;
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