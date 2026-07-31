using System;
using FarmClaim.Domain.Enums;

namespace FarmClaim.Application.Features.InsurancePolicies.DTOs
{
    public class PremiumScheduleDto
    {
        public Guid Id { get; set; }
        public Guid PolicyId { get; set; }
        public int InstallmentNumber { get; set; }
        public DateTime DueDate { get; set; }
        public decimal AmountDue { get; set; }
        public Guid? PaymentId { get; set; }
        public PremiumScheduleStatus Status { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}
