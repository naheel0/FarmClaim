using FarmClaim.Application.Features.Claims.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Claims.Commands.UpdateClaim
{
    public record UpdateClaimCommand(
        Guid ClaimId,
        Guid UserId,
        UpdateClaimRequestDto Request
    ) : IRequest<ClaimResponseDto>;
}