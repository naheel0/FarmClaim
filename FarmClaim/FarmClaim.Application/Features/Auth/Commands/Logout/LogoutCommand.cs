using System;
using MediatR;

namespace FarmClaim.Application.Features.Auth.Commands.Logout
{
    public record LogoutCommand(Guid UserId) : IRequest<bool>;
}