using FarmClaim.Application.Common.DTOs;
using FarmClaim.Application.Features.InsurancePlans.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.InsurancePlans.Queries.GetAllPlans
{
    public record GetAllPlansQuery(
        int PageNumber = 1,
        int PageSize = 20,
        string? SearchTerm = null,
        string? CropType = null,
        bool? IsActive = null,
        bool AdminContext = false
    ) : IRequest<PagedResult<PlanListDto>>;
}