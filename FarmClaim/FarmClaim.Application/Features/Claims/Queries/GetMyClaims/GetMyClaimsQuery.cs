using FarmClaim.Application.Common.DTOs;
using FarmClaim.Application.Features.Claims.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Claims.Queries.GetMyClaims
{
    public record GetMyClaimsQuery(
        Guid UserId,
        int PageNumber = 1,
        int PageSize = 20,
        string? StatusFilter = null,
        string? SearchTerm = null
    ) : IRequest<PagedResult<ClaimListDto>>;
}