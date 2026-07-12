using MediatR;
using FarmClaim.Application.Features.Farms.DTOs;

namespace FarmClaim.Application.Features.Farms.Commands.UpdateFarm
{
    public record UpdateFarmCommand(Guid FarmId, Guid UserId, UpdateFarmRequestDto Request) : IRequest<FarmResponseDto>;
}