using FarmClaim.Application.Features.Farmers.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Farmers.Queries.GetCurrentUserProfile;

public class GetCurrentUserProfileQuery : IRequest<FarmerProfileDto>
{
    public Guid UserId { get; }

    public GetCurrentUserProfileQuery(Guid userId)
    {
        UserId = userId;
    }
}