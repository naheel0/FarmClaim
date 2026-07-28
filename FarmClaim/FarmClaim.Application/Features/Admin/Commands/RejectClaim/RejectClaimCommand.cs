using FarmClaim.Application.Features.Admin.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Admin.Commands.RejectClaim
{
    public record RejectClaimCommand(Guid ClaimId, Guid AdminId, string AdminEmail, RejectClaimRequestDto Request) : IRequest;
}
