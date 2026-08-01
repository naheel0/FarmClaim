using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Infrastructure.Jobs;
using Hangfire;

namespace FarmClaim.Infrastructure.Services
{
    public class HangfireBackgroundJobService : IClaimBackgroundJobService
    {
        private readonly IBackgroundJobClient _backgroundJobClient;

        public HangfireBackgroundJobService(IBackgroundJobClient backgroundJobClient)
        {
            _backgroundJobClient = backgroundJobClient;
        }

        public void EnqueueWeatherAnalysis(Guid claimId)
        {
            _backgroundJobClient.Enqueue<ClaimBackgroundJobService>(
                x => x.ProcessWeatherAnalysisAsync(claimId));
        }

        public void EnqueueAIAnalysis(Guid claimId)
        {
            _backgroundJobClient.Enqueue<ClaimBackgroundJobService>(
                x => x.ProcessAIAnalysisAsync(claimId));
        }
    }
}