using MediatR;

namespace FarmClaim.Application.Features.Claims.Commands.DeleteClaimImage
{
    public record DeleteClaimImageCommand(
        Guid ClaimId,
        Guid ImageId,
        Guid UserId
    ) : IRequest<Unit>;
}
