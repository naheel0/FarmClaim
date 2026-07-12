using MediatR;

namespace FarmClaim.Application.Features.Claims.Commands.DeleteClaim
{
    public record DeleteClaimCommand(
        Guid ClaimId,
        Guid UserId
    ) : IRequest<Unit>;
}