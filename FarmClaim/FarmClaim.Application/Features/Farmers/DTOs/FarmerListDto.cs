using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarmClaim.Application.Features.Farmers.DTOs
{
    public record FarmerListDto
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FirstName {  get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string? PhoneNumber {  get; init; }
        public string Role {  get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? LastLoginAt { get; init; }
        public int FarmsCount { get; init; }
        public int PoliciesCount { get; init; }
        public int ClaimsCount { get; init; }
    }

}
