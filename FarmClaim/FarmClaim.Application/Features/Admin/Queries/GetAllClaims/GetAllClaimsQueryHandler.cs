using FarmClaim.Application.Common.DTOs;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Admin.DTOs;
using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Admin.Queries.GetAllClaims
{
    public class GetAllClaimsQueryHandler : IRequestHandler<GetAllClaimsQuery, PagedResult<AdminClaimListDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<GetAllClaimsQueryHandler> _logger;

        public GetAllClaimsQueryHandler(IApplicationDbContext context, ILogger<GetAllClaimsQueryHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PagedResult<AdminClaimListDto>> Handle(GetAllClaimsQuery request, CancellationToken ct)
        {
            IQueryable<Claim> queryable = _context.Claims
                .AsNoTracking()
                .Include(c => c.Policy)
                .Include(c => c.Farm).ThenInclude(f => f!.User)
                .Include(c => c.Images)
                .Where(c => !c.IsDeleted);

            // Filters
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (Enum.TryParse<ClaimStatus>(request.Status.Trim(), true, out var status))
                    queryable = queryable.Where(c => c.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(request.IncidentType))
            {
                if (Enum.TryParse<IncidentType>(request.IncidentType.Trim(), true, out var incidentType))
                    queryable = queryable.Where(c => c.IncidentType == incidentType);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();

                // Pre-parse a matching incident type so we avoid calling .ToString()
                // inside a LINQ-to-EF expression, which EF Core cannot translate (causes a 500).
                IncidentType? matchedIncidentType = null;
                foreach (IncidentType t in Enum.GetValues<IncidentType>())
                {
                    if (t.ToString().ToLower().Contains(term))
                    {
                        matchedIncidentType = t;
                        break;
                    }
                }

                queryable = queryable.Where(c =>
                    (c.Policy != null && c.Policy.PolicyNumber != null && c.Policy.PolicyNumber.ToLower().Contains(term)) ||
                    (c.Farm != null && c.Farm.Name != null && c.Farm.Name.ToLower().Contains(term)) ||
                    (c.Farm != null && c.Farm.User != null &&
                     (c.Farm.User.FirstName != null && c.Farm.User.FirstName.ToLower().Contains(term) ||
                      c.Farm.User.LastName != null && c.Farm.User.LastName.ToLower().Contains(term))) ||
                    (c.Farm != null && c.Farm.User != null && c.Farm.User.Email != null && c.Farm.User.Email.ToLower().Contains(term)) ||
                    (matchedIncidentType != null && c.IncidentType == matchedIncidentType));
            }

            // Sorting
            var isDesc = string.Equals(request.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);
            queryable = (request.SortBy?.ToLower()) switch
            {
                "incidentdate" => isDesc ? queryable.OrderByDescending(c => c.IncidentDate) : queryable.OrderBy(c => c.IncidentDate),
                "status" => isDesc ? queryable.OrderByDescending(c => c.Status) : queryable.OrderBy(c => c.Status),
                "amount" => isDesc ? queryable.OrderByDescending(c => c.ApprovedAmount) : queryable.OrderBy(c => c.ApprovedAmount),
                _ => isDesc ? queryable.OrderByDescending(c => c.CreatedAt) : queryable.OrderBy(c => c.CreatedAt)
            };

            var totalCount = await queryable.CountAsync(ct);

            var items = await queryable
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(c => new AdminClaimListDto
                {
                    Id = c.Id,
                    PolicyId = c.PolicyId,
                    FarmId = c.FarmId,
                    UserId = c.UserId,
                    FarmerName = c.Farm != null && c.Farm.User != null
                        ? c.Farm.User.FirstName + " " + c.Farm.User.LastName
                        : string.Empty,
                    FarmerEmail = c.Farm != null && c.Farm.User != null ? c.Farm.User.Email : string.Empty,
                    PolicyNumber = c.Policy != null ? c.Policy.PolicyNumber : string.Empty,
                    FarmName = c.Farm != null ? c.Farm.Name : string.Empty,
                    CropType = c.Policy != null ? c.Policy.CropType : string.Empty,
                    SumInsured = c.Policy != null ? c.Policy.SumInsured : 0,
                    IncidentDate = c.IncidentDate,
                    IncidentType = c.IncidentType,
                    Status = c.Status,
                    ApprovedAmount = c.ApprovedAmount,
                    ImageCount = c.Images.Count(i => !i.IsDeleted),
                        // M6 FIX: Exclude error fallback JSON from "has real AI analysis" check
                        // IsError is serialized as "isError":true in the JSON
                        HasAIAnalysis = !string.IsNullOrEmpty(c.AIAnalysisResult)
                            && !c.AIAnalysisResult.Contains("\"isError\""),
                    HasWeatherData = c.WeatherSnapshot != null,
                    CreatedAt = c.CreatedAt
                }).ToListAsync(ct);

            var totalPages = request.PageSize > 0 ? (int)Math.Ceiling((double)totalCount / request.PageSize) : 0;

            return new PagedResult<AdminClaimListDto>
            {
                Items = items,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }
    }
}