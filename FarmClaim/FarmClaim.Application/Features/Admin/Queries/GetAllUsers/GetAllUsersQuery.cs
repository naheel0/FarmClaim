using FarmClaim.Application.Common.DTOs;
using FarmClaim.Application.Features.Admin.DTOs;
using FarmClaim.Domain.Enums;
using MediatR;

namespace FarmClaim.Application.Features.Admin.Queries.GetAllUsers
{
    public record GetAllUsersQuery(
        int PageNumber = 1,
        int PageSize = 20,
        string? SearchTerm = null,
        UserRole? Role = null,
        UserStatus? Status = null,
        string? SortBy = "CreatedAt",
        string? SortOrder = "desc"
    ) : IRequest<PagedResult<AdminUserListDto>>;
}