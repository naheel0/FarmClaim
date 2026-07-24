using Polly;
using Polly.Extensions.Http;
using System.Net;

namespace FarmClaim.Infrastructure.Email.Policies
{
    public static class EmailRetryPolicy
    {
        public static IAsyncPolicy EmailPolicy =>
            Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        Console.WriteLine($"[Email Retry] Attempt {retryCount}/3 after {timespan.TotalSeconds}s");
                    });
    }
}