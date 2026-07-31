using FarmClaim.Application.Common.DTOs;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Admin.DTOs;
using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Admin.Queries.GetAllPolicies
{
    public class GetAllPoliciesQueryHandler : IRequestHandler<GetAllPoliciesQuery, PagedResult<AdminPolicyListDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<GetAllPoliciesQueryHandler> _logger;

        public GetAllPoliciesQueryHandler(IApplicationDbContext context, ILogger<GetAllPoliciesQueryHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PagedResult<AdminPolicyListDto>> Handle(GetAllPoliciesQuery request, CancellationToken ct)
        {
            IQueryable<InsurancePolicy> queryable = _context.InsurancePolicies
                .AsNoTracking()
                .Include(p => p.Farm).ThenInclude(f => f!.User)
                .Include(p => p.ApprovedByUser)
                .Include(p => p.Claims)
                .Include(p => p.Payments)
                .Where(p => !p.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (Enum.TryParse<PolicyStatus>(request.Status.Trim(), true, out var status))
                    queryable = queryable.Where(p => p.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                queryable = queryable.Where(p =>
                    p.PolicyNumber.ToLower().Contains(term) ||
                    (p.Farm != null && p.Farm.Name.ToLower().Contains(term)) ||
                    (p.Farm != null && p.Farm.User != null && (p.Farm.User.FirstName + " " + p.Farm.User.LastName).ToLower().Contains(term)) ||
                    (p.Farm != null && p.Farm.User != null && p.Farm.User.Email.ToLower().Contains(term)) ||
                    p.Provider.ToLower().Contains(term) ||
                    p.CropType.ToLower().Contains(term));
            }

            var isDesc = string.Equals(request.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);
            queryable = (request.SortBy?.ToLower()) switch
            {
                "status" => isDesc ? queryable.OrderByDescending(p => p.Status) : queryable.OrderBy(p => p.Status),
                "premium" => isDesc ? queryable.OrderByDescending(p => p.Premium) : queryable.OrderBy(p => p.Premium),
                "startdate" => isDesc ? queryable.OrderByDescending(p => p.StartDate) : queryable.OrderBy(p => p.StartDate),
                _ => isDesc ? queryable.OrderByDescending(p => p.CreatedAt) : queryable.OrderBy(p => p.CreatedAt)
            };

            var totalCount = await queryable.CountAsync(ct);

            var items = await queryable
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new AdminPolicyListDto
                {
                    Id = p.Id,
                    FarmId = p.FarmId,
                    UserId = p.Farm != null && p.Farm.User != null ? p.Farm.UserId : Guid.Empty,
                    FarmerName = p.Farm != null && p.Farm.User != null
                        ? p.Farm.User.FirstName + " " + p.Farm.User.LastName
                        : string.Empty,
                    FarmerEmail = p.Farm != null && p.Farm.User != null ? p.Farm.User.Email : string.Empty,
                    FarmName = p.Farm != null ? p.Farm.Name : string.Empty,
                    PolicyNumber = p.PolicyNumber,
                    Provider = p.Provider,
                    CropType = p.CropType,
                    CoverageAmount = p.CoverageAmount,
                    Premium = p.Premium,
                    SumInsured = p.SumInsured,
                    Status = p.Status,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    ApprovedAt = p.ApprovedAt,
                    ApprovedByName = p.ApprovedByUser != null
                        ? p.ApprovedByUser.FirstName + " " + p.ApprovedByUser.LastName
                        : null,
                    RejectedAt = p.RejectedAt,
                    RejectionReason = p.RejectionReason,
                    ClaimsCount = p.Claims.Count(c => !c.IsDeleted),
                    CreatedAt = p.CreatedAt,
                    PaymentStatus = p.Payments.Any(p => p.Status == PaymentStatus.Captured && !p.IsDeleted)
                        ? "Paid" : "Unpaid",
                    CurrentInstallmentNumber = p.CurrentInstallmentNumber,
                    NextInstallmentDueDate = p.NextInstallmentDueDate,
                    InstallmentAmount = p.InstallmentAmount
                }).ToListAsync(ct);

            var totalPages = request.PageSize > 0 ? (int)Math.Ceiling((double)totalCount / request.PageSize) : 0;

            return new PagedResult<AdminPolicyListDto>
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
