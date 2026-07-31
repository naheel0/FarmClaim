using FarmClaim.Application.Common.DTOs;
using FarmClaim.Application.Features.Admin.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Admin.Queries.GetAllPolicies
{
    public record GetAllPoliciesQuery(
        int PageNumber = 1,
        int PageSize = 20,
        string? Status = null,
        string? SearchTerm = null,
        string? SortBy = "CreatedAt",
        string? SortOrder = "desc"
    ) : IRequest<PagedResult<AdminPolicyListDto>>;
}
