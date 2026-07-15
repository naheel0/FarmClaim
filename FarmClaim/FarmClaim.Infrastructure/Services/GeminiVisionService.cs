using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FarmClaim.Application.Common.Constants;
using FarmClaim.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Infrastructure.Services
{
    public class GeminiVisionService : IGeminiVisionService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiVisionService> _logger;
        private readonly string _apiKey;

        public GeminiVisionService(HttpClient httpClient, IConfiguration config, ILogger<GeminiVisionService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = config["GeminiVision:ApiKey"]
                ?? throw new InvalidOperationException("GeminiVision:ApiKey is not configured.");
        }

        public async Task<AIAnalysisResult> AnalyzeImagesAsync(
            List<string> imageUrls, string cropType, CancellationToken ct = default)
        {
            if (imageUrls is not { Count: > 0 })
                throw new ArgumentException("At least one image URL is required.", nameof(imageUrls));

            _logger.LogInformation("Starting Gemini analysis for {Count} images, crop: {Crop}", imageUrls.Count, cropType);

            var prompt = PromptTemplates.CropDamageAnalysis.Replace("{CropType}", cropType);

            var parts = new List<object>
            {
                new { text = prompt }
            };

            foreach (var imageUrl in imageUrls)
            {
                var base64 = await DownloadAsBase64Async(imageUrl, ct);
                if (string.IsNullOrEmpty(base64))
                {
                    _logger.LogWarning("Skipping invalid image: {Url}", imageUrl);
                    continue;
                }

                parts.Add(new
                {
                    inline_data = new { mime_type = "image/jpeg", data = base64 }
                });
            }

            var requestBody = new
            {
                contents = new[] { new { parts } },
                generationConfig = new { temperature = 0.2, topP = 0.8, maxOutputTokens = 1024 }
            };

            var url = $"v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}";

            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, requestBody, ct);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
                var text = json
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? string.Empty;

                _logger.LogDebug("Gemini raw response: {Text}", text);
                return ParseResponse(text);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Gemini API request failed");
                throw new InvalidOperationException("Failed to reach Gemini Vision API. Check your API key and network.", ex);
            }
        }

        private async Task<string> DownloadAsBase64Async(string imageUrl, CancellationToken ct)
        {
            try
            {
                var bytes = await _httpClient.GetByteArrayAsync(imageUrl, ct);
                return Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to download image: {Url}", imageUrl);
                return string.Empty;
            }
        }

        private static AIAnalysisResult ParseResponse(string text)
        {
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');

            if (start < 0 || end <= start)
                return Fallback(text);

            try
            {
                var json = text.Substring(start, end - start + 1);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                return new AIAnalysisResult
                {
                    DamagePercentage = root.GetProperty("damagePercentage").GetDouble(),
                    DamageDescription = root.GetProperty("damageDescription").GetString() ?? string.Empty,
                    Confidence = root.GetProperty("confidence").GetString() ?? "Low",
                    RawResponse = text
                };
            }
            catch (JsonException)
            {
                return Fallback(text);
            }
        }

        private static AIAnalysisResult Fallback(string raw)
        {
            return new AIAnalysisResult
            {
                DamagePercentage = 0,
                DamageDescription = "AI analysis could not be parsed.",
                Confidence = "Low",
                RawResponse = raw
            };
        }
    }
}