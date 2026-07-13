using FarmClaim.Application.Common.DTOs;
using FarmClaim.Application.Features.Farmers.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Farmers.Queries.GetAllFarmers
{
    public record GetAllFarmersQuery(
        int PageNumber = 1,
        int PageSize = 20,
        string? SearchTerm = null
    ) : IRequest<PagedResult<FarmerListDto>>;
}