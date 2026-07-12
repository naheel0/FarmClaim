using FarmClaim.Application.Common.DTOs;
using FarmClaim.Application.Features.Farms.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Farms.Queries.GetMyFarms
{
    /// <summary>
    /// Query to retrieve paginated list of farms for current user
    /// </summary>
    public record GetMyFarmsQuery(
        Guid UserId,
        int PageNumber = 1,
        int PageSize = 20,
        string? SearchTerm = null
    ) : IRequest<PagedResult<FarmListDto>>;
}