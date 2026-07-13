using FarmClaim.Application.Features.Farms.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Farms.Commands.CreateFarm;

public class CreateFarmCommand : IRequest<FarmResponseDto>
{
    public Guid UserId { get; }
    public CreateFarmRequestDto Request { get; }

    public CreateFarmCommand(Guid userId, CreateFarmRequestDto request)
    {
        UserId = userId;
        Request = request;
    }
}