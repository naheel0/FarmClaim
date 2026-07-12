using FarmClaim.Application.Features.Farmers.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Farmers.Commands.UpdateProfile
{
    public record UpdateProfileCommand(Guid UserId, UpdateProfileRequestDto Request) : IRequest<FarmerProfileDto>;
}
