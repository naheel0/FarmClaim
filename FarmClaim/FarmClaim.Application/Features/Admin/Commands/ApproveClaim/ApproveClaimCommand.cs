using FarmClaim.Application.Features.Admin.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Admin.Commands.ApproveClaim
{
    public record ApproveClaimCommand(Guid ClaimId, Guid AdminId, string AdminEmail, ApproveClaimRequestDto Request) : IRequest;
}
