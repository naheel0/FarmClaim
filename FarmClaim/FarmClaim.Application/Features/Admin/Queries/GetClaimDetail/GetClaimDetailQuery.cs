using FarmClaim.Application.Features.Admin.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Admin.Queries.GetClaimDetail
{
    public record GetClaimDetailQuery(Guid ClaimId) : IRequest<AdminClaimDetailDto>;
}