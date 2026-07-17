using FarmClaim.Application.Features.Admin.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Admin.Queries.GetDashboardStats
{
    public record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;
}