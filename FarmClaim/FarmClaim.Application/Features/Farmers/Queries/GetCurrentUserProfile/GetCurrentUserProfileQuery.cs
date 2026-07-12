using FarmClaim.Application.Features.Farmers.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Farmers.Queries.GetCurrentUserProfile
{
    public record GetCurrentUserProfileQuery(Guid UserId) : IRequest<FarmerProfileDto>;
}
