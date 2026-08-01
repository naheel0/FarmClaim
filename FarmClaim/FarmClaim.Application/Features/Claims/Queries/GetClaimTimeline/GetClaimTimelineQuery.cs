using FarmClaim.Application.Features.Claims.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Claims.Queries.GetClaimTimeline
{
    public record GetClaimTimelineQuery(
        Guid ClaimId,
        Guid UserId
    ) : IRequest<List<ClaimTimelineEntryDto>>;
}
