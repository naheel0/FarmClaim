using FarmClaim.Application.Features.Farms.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Farms.Queries.GetFarmById
{
    /// <summary>
    /// Query to retrieve a single farm by its ID with ownership validation
    /// </summary>
    public record GetFarmByIdQuery(
        Guid FarmId,
        Guid UserId
    ) : IRequest<FarmResponseDto>;
}