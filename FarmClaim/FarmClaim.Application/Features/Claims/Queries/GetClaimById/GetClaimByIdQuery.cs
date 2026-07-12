using FarmClaim.Application.Features.Claims.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Claims.Queries.GetClaimById
{
    public record GetClaimByIdQuery(
        Guid ClaimId,
        Guid UserId
    ) : IRequest<ClaimResponseDto>;
}