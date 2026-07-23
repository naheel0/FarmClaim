using System;
using FarmClaim.Domain.Enums;

namespace FarmClaim.Application.Features.Admin.DTOs
{
    public record AdminUserDetailDto
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string? PhoneNumber { get; init; }
        public UserRole Role { get; init; }
        public UserStatus Status { get; init; }
        public DateTime? LastLoginAt { get; init; }
        public DateTime? StatusChangedAt { get; init; }
        public Guid? StatusChangedByUserId { get; init; }
        public string? StatusChangedByName { get; init; }
        public string? StatusChangeReason { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public int FarmsCount { get; init; }
        public int PoliciesCount { get; init; }
        public int ClaimsCount { get; init; }
    }
}