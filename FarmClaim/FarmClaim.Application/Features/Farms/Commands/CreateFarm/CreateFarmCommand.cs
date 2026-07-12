using MediatR;
using FarmClaim.Application.Features.Farms.DTOs;

namespace FarmClaim.Application.Features.Farms.Commands.CreateFarm
{
    public record CreateFarmCommand(Guid UserId, CreateFarmRequestDto Request) : IRequest<FarmResponseDto>;
}