using FarmClaim.Application.Common.DTOs;
using FarmClaim.Application.Features.Admin.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Admin.Queries.GetAllClaims
{
    public record GetAllClaimsQuery(
        int PageNumber = 1,
        int PageSize = 20,
        string? Status = null,
        string? IncidentType = null,
        string? SearchTerm = null,
        string? SortBy = "CreatedAt",
        string? SortOrder = "desc"
    ) : IRequest<PagedResult<AdminClaimListDto>>;
}