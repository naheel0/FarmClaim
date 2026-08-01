using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
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
        private const int MaxImages = 5;
        private const int MaxImageBytes = 5 * 1024 * 1024; // 5 MB per image

        private readonly HttpClient _httpClient;
        private readonly HttpClient _downloadClient;
        private readonly ILogger<GeminiVisionService> _logger;
        private readonly string _apiKey;

        private static readonly HashSet<string> AllowedImageHosts = new(StringComparer.OrdinalIgnoreCase)
        {
            "res.cloudinary.com",
        };

        public GeminiVisionService(HttpClient httpClient, IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<GeminiVisionService> logger)
        {
            _httpClient = httpClient;
            _downloadClient = httpClientFactory.CreateClient("GeminiDownload");
            _logger = logger;
            _apiKey = config["GeminiVision:ApiKey"]
                ?? throw new InvalidOperationException("GeminiVision:ApiKey is not configured.");

            // API key only on the Gemini API client — NOT on the download client
            _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", _apiKey);
        }

        public async Task<AIAnalysisResult> AnalyzeImagesAsync(
            List<string> imageUrls, string cropType, CancellationToken ct = default)
        {
            if (imageUrls is not { Count: > 0 })
                throw new ArgumentException("At least one image URL is required.", nameof(imageUrls));

            if (imageUrls.Count > MaxImages)
            {
                _logger.LogInformation("Truncating {Count} images to max {Max}", imageUrls.Count, MaxImages);
                imageUrls = imageUrls.Take(MaxImages).ToList();
            }

            _logger.LogInformation("Starting Gemini analysis for {Count} images, crop: {Crop}", imageUrls.Count, cropType);

            var prompt = PromptTemplates.CropDamageAnalysis.Replace("{CropType}", cropType);

            var parts = new List<object>
            {
                new { text = prompt }
            };

            foreach (var imageUrl in imageUrls)
            {
                var (base64, mimeType) = await DownloadAsBase64Async(imageUrl, ct);
                if (string.IsNullOrEmpty(base64))
                {
                    _logger.LogWarning("Skipping invalid image: {Url}", imageUrl);
                    continue;
                }

                parts.Add(new
                {
                    inline_data = new { mime_type = mimeType, data = base64 }
                });
            }

            var requestBody = new
            {
                contents = new[] { new { parts } },
                generationConfig = new { temperature = 0.2, topP = 0.8, maxOutputTokens = 1024 }
            };

            var url = "v1beta/models/gemini-2.0-flash:generateContent";

            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, requestBody, ct);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

                // Log token usage for cost monitoring
                if (json.TryGetProperty("usageMetadata", out var usage))
                {
                    _logger.LogInformation("Gemini token usage — input: {Input}, output: {Output}, total: {Total}",
                        usage.TryGetProperty("promptTokenCount", out var p) ? p.GetInt32() : -1,
                        usage.TryGetProperty("candidatesTokenCount", out var c) ? c.GetInt32() : -1,
                        usage.TryGetProperty("totalTokenCount", out var t) ? t.GetInt32() : -1);
                }

                // C11 FIX: Safe property access — Gemini may return safety-blocked or error responses
                // that have no "candidates" key or an empty array, causing KeyNotFoundException/IndexOutOfRangeException.
                if (!json.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                {
                    // Check for safety block or error payload
                    var blockReason = "";
                    if (json.TryGetProperty("promptFeedback", out var pf) &&
                        pf.TryGetProperty("blockReason", out var br))
                    {
                        blockReason = br.GetString() ?? "unknown";
                    }
                    var errorMsg = $"Gemini returned no candidates (blockReason: {blockReason})";
                    _logger.LogWarning("Gemini analysis failed: {Error}", errorMsg);
                    return Fallback(errorMsg);
                }

                var candidate = candidates[0];

                // Check for safety ratings indicating blocked content
                if (candidate.TryGetProperty("finishReason", out var finishReason) &&
                    finishReason.GetString() == "SAFETY")
                {
                    _logger.LogWarning("Gemini analysis blocked by safety filter");
                    return Fallback("Analysis blocked by content safety filter");
                }

                var text = candidate
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

        private async Task<(string base64, string mimeType)> DownloadAsBase64Async(string imageUrl, CancellationToken ct)
        {
            try
            {
                // SSRF protection: only allow known image hosts
                if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) ||
                    !AllowedImageHosts.Contains(uri.Host))
                {
                    _logger.LogWarning("Rejected non-allowed image host: {Url}", imageUrl);
                    return (string.Empty, string.Empty);
                }

                var bytes = await _downloadClient.GetByteArrayAsync(uri, ct);

                if (bytes.Length > MaxImageBytes)
                {
                    _logger.LogWarning("Skipping oversized image ({Bytes} bytes): {Url}", bytes.Length, imageUrl);
                    return (string.Empty, string.Empty);
                }

                if (bytes.Length == 0)
                {
                    _logger.LogWarning("Empty image downloaded: {Url}", imageUrl);
                    return (string.Empty, string.Empty);
                }

                var mimeType = DetectMimeType(bytes, uri);
                var base64 = Convert.ToBase64String(bytes);
                return (base64, mimeType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to download image: {Url}", imageUrl);
                return (string.Empty, string.Empty);
            }
        }

        private static string DetectMimeType(byte[] bytes, Uri uri)
        {
            // Check magic bytes for common image formats
            if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8)
                return "image/jpeg";
            if (bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                return "image/png";
            if (bytes.Length >= 4 && bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x38)
                return "image/gif";
            if (bytes.Length >= 2 && bytes[0] == 0x42 && bytes[1] == 0x4D)
                return "image/bmp";
            if (bytes.Length >= 12 && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46
                && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
                return "image/webp";

            // Fallback: derive from URL extension
            var ext = Path.GetExtension(uri.LocalPath)?.ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };
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

                double? damagePct = null;
                if (root.TryGetProperty("damagePercentage", out var dp))
                {
                    var raw = dp.GetDouble();
                    damagePct = Math.Clamp(raw, 0, 100);  // H3: clamp to valid range
                }

                return new AIAnalysisResult
                {
                    DamagePercentage = damagePct,
                    DamageDescription = root.TryGetProperty("damageDescription", out var dd)
                        ? dd.GetString() ?? string.Empty : string.Empty,
                    Confidence = NormalizeConfidence(root.TryGetProperty("confidence", out var cf)
                        ? cf.GetString() ?? null : null),
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
                DamagePercentage = null,
                DamageDescription = "AI analysis could not be parsed.",
                Confidence = "Low",
                RawResponse = raw,
                // M6 FIX: Include error flag so admin query filter can detect parse failures.
                // Serialized JSON will contain "error":true which the query checks for.
                IsError = true
            };
        }

        // M5 FIX: Normalize confidence to expected values — Gemini can return any string
        private static string NormalizeConfidence(string? raw)
        {
            return raw?.ToLowerInvariant() switch
            {
                "high" => "High",
                "medium" => "Medium",
                "low" => "Low",
                _ => "Low" // default for unrecognized values
            };
        }
    }
}
