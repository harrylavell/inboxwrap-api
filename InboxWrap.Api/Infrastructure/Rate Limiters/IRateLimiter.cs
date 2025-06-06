namespace InboxWrap.Infrastructure.RateLimiters;

public interface IRateLimiter
{
    Task WaitForAvailabilityAsync(int tokenCost, CancellationToken ct = default);
}
