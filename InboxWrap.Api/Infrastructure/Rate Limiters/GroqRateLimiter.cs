namespace InboxWrap.Infrastructure.RateLimiters;

// Token bucket
public class GroqRateLimiter : IRateLimiter
{
    private readonly object _lock = new();
    private readonly int _maxRequestsPerMinute;
    private readonly int _maxTokensPerMinute;

    private int _remainingRequests;
    private int _remainingTokens;

    private DateTime _lastRefill;

    public GroqRateLimiter(int rpm, int tpm)
    {
        _maxRequestsPerMinute = rpm;
        _maxTokensPerMinute = tpm;

        _remainingRequests = rpm;
        _remainingTokens = tpm;
        _lastRefill = DateTime.UtcNow;
    }

    public async Task WaitForAvailabilityAsync(int tokenCost, CancellationToken ct = default)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            lock (_lock)
            {
                RefillIfNecessary();

                if (_remainingRequests > 0 && _remainingTokens >= tokenCost)
                {
                    _remainingRequests--;
                    _remainingTokens -= tokenCost;
                    return;
                }
            }

            await Task.Delay(200, ct); // retry after small delay
        }
    }

    private void RefillIfNecessary()
    {
        var now = DateTime.UtcNow;
        if ((now - _lastRefill).TotalSeconds >= 60)
        {
            _remainingRequests = _maxRequestsPerMinute;
            _remainingTokens = _maxTokensPerMinute;
            _lastRefill = now;
        }
    }
}
