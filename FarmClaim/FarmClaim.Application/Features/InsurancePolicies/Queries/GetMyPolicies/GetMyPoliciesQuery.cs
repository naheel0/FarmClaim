using FarmClaim.Application.Common.DTOs;
using FarmClaim.Application.Features.InsurancePolicies.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.InsurancePolicies.Queries.GetMyPolicies
{
    public record GetMyPoliciesQuery(
        Guid UserId,
        int PageNumber = 1,
        int PageSize = 20,
        string? SearchTerm = null
    ) : IRequest<PagedResult<PolicyListDto>>;
}