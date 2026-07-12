using FarmClaim.Application.Features.Claims.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Claims.Commands.CreateClaim
{
    public record CreateClaimCommand(
        Guid UserId,
        CreateClaimRequestDto Request
    ) : IRequest<ClaimResponseDto>;
}