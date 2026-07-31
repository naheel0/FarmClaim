using System.ComponentModel.DataAnnotations;

namespace FarmClaim.Application.Features.Payments.DTOs
{
    public class CreateOrderRequestDto
    {
        [Range(1, double.MaxValue)]
        public decimal? CustomAmount { get; set; }

        public Guid? PremiumScheduleId { get; set; }
    }
}