using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FarmClaim.Domain.Enums;

namespace FarmClaim.Application.Features.InsurancePlans.Queries.GetAllPlans
{
    public class PlanListDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CropType { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public decimal PremiumRatePerHectare { get; set; }
        public decimal SumInsuredPerHectare { get; set; }
        public decimal CoveragePercentage { get; set; }
        public int PolicyDurationMonths { get; set; }
        public bool IsActive { get; set; }
        public int PoliciesCount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        // Installment support
        public bool SupportsInstallments { get; set; }
        public int? InstallmentCount { get; set; }
        public FarmClaim.Domain.Enums.InstallmentFrequency? InstallmentFrequency { get; set; }
    }
}
