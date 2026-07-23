using FarmClaim.Application.Features.Admin.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Admin.Queries.GetUserById
{
    public record GetUserByIdQuery(Guid UserId) : IRequest<AdminUserDetailDto>;
}