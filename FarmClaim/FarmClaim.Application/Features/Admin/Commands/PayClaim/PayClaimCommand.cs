using FarmClaim.Application.Features.Admin.DTOs;
using FarmClaim.Application.Features.Claims.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Admin.Commands.PayClaim
{
    public record PayClaimCommand(
        Guid ClaimId,
        Guid AdminUserId,
        PayClaimRequestDto Request
    ) : IRequest<ClaimResponseDto>;
}