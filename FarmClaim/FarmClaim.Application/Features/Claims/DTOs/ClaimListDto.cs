using System;

namespace FarmClaim.Application.Features.Claims.DTOs
{
    public record ClaimListDto
    {
        public Guid Id { get; init; }
        public Guid PolicyId { get; init; }
        public Guid FarmId { get; init; }
        public string PolicyNumber { get; init; } = string.Empty;
        public string FarmName { get; init; } = string.Empty;
        public DateTime IncidentDate { get; init; }
        public string IncidentType { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public decimal? ApprovedAmount { get; init; }
        public DateTime CreatedAt { get; init; }
        public int ImageCount { get; init; }
    }
}