using MediatR;

namespace FarmClaim.Application.Features.Admin.Commands.SetUnderReview
{
    public record SetUnderReviewCommand(Guid ClaimId, Guid AdminId, string AdminEmail) : IRequest;
}
