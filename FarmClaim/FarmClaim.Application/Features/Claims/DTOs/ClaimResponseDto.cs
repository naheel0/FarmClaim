using System;
using System.Collections.Generic;
using FarmClaim.Domain.Enums;

namespace FarmClaim.Application.Features.Claims.DTOs
{
    public record ClaimResponseDto
    {
        public Guid Id { get; init; }
        public Guid PolicyId { get; init; }
        public Guid FarmId { get; init; }
        public Guid UserId { get; init; }
        public string PolicyNumber { get; init; } = string.Empty;
        public string FarmName { get; init; } = string.Empty;
        public DateTime IncidentDate { get; init; }
        public IncidentType IncidentType { get; init; }
        public string? Description { get; init; }
        public string? DamageDescription { get; init; }
        public ClaimStatus Status { get; init; }
        public decimal? ApprovedAmount { get; init; }

        // Review tracking
        public string? ReviewedBy { get; init; }
        public DateTime? ReviewedAt { get; init; }
        public string? RejectionReason { get; init; }

        // NEW: Admin user who reviewed
        public Guid? ReviewedByUserId { get; init; }
        public string? ReviewedByName { get; init; }

        // NEW: Payment tracking
        public DateTime? PaidAt { get; init; }
        public string? PaymentReference { get; init; }

        // AI & Weather (unchanged)
        public string? WeatherSnapshot { get; init; }
        public string? AIAnalysisResult { get; init; }

        // PROD: Structured verification state
        public string? WeatherStatus { get; init; }
        public string? WeatherErrorMessage { get; init; }
        public string? AIAnalysisStatus { get; init; }
        public string? AIErrorMessage { get; init; }

        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public List<ClaimImageDto> Images { get; init; } = new();
    }
}