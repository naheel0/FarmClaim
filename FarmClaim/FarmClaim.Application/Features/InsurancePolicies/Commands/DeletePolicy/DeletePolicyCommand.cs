using MediatR;

namespace FarmClaim.Application.Features.InsurancePolicies.Commands.DeletePolicy
{
    public record DeletePolicyCommand(
        Guid PolicyId,
        Guid UserId
    ) : IRequest<Unit>;
}