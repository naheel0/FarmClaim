namespace FarmClaim.Application.Common.Interfaces
{
    public interface IClaimBackgroundJobService
    {
        void EnqueueWeatherAnalysis(Guid claimId);
        void EnqueueAIAnalysis(Guid claimId);
    }
}