using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FarmClaim.Application.Common.Interfaces
{
    public interface IGeminiVisionService
    {
        Task<AIAnalysisResult> AnalyzeImagesAsync(List<string> imageUrls, string cropType, CancellationToken ct = default);
    }

    public class AIAnalysisResult
    {
        public double? DamagePercentage { get; set; }
        public string DamageDescription { get; set; } = string.Empty;
        public string Confidence { get; set; } = string.Empty;
        public string RawResponse { get; set; } = string.Empty;
        // M6 FIX: Error marker — when true, admin query filters exclude this from "has AI" counts
        public bool IsError { get; set; }
    }
}