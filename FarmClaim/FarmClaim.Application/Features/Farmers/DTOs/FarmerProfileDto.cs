namespace FarmClaim.Application.Features.Farmers.DTOs
{
    public record FarmerProfileDto
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string? PhoneNumber { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? LastLoginAt { get; init; }
        public int TotalFarms { get; init; }
        public int TotalPolicies { get; init; }
        public int TotalClaims { get; init; }
    }
}
