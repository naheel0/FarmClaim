using System.Threading;
using System.Threading.Tasks;

namespace FarmClaim.Application.Common.Interfaces
{
    public interface IWeatherService
    {
        Task<WeatherSnapshot> GetWeatherAsync(double latitude, double longitude, DateTime date, CancellationToken ct = default);
    }

    public class WeatherSnapshot
    {
        public DateTime Date { get; set; }
        public double TemperatureCelsius { get; set; }
        public double RainfallMm { get; set; }
        public double WindSpeedKmh { get; set; }
        public string WeatherCondition { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
    }
}