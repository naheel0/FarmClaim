using System;
using FarmClaim.Domain.Enums;

namespace FarmClaim.Application.Features.Admin.DTOs
{
    public record UserActionResponseDto
    {
        public Guid UserId { get; init; }
        public string Email { get; init; } = string.Empty;
        public UserStatus PreviousStatus { get; init; }
        public UserStatus NewStatus { get; init; }
        public DateTime? StatusChangedAt { get; init; }
        public Guid? StatusChangedByUserId { get; init; }
        public string? StatusChangedByName { get; init; }
        public string? Reason { get; init; }
    }
}