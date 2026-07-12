using System;
using System.Collections.Generic;

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
        public string IncidentType { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string? DamageDescription { get; init; }
        public string Status { get; init; } = string.Empty;
        public decimal? ApprovedAmount { get; init; }
        public string? ReviewedBy { get; init; }
        public DateTime? ReviewedAt { get; init; }
        public string? RejectionReason { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public List<ClaimImageDto> Images { get; init; } = new();
    }
}