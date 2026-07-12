using MediatR;

namespace FarmClaim.Application.Features.Farms.Commands.DeleteFarm
{
    public record DeleteFarmCommand(Guid FarmId, Guid UserId) : IRequest<bool>;
}