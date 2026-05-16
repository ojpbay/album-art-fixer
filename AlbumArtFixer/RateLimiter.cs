namespace AlbumArtFixer;

// Simple token-bucket rate limiter for sequential (non-parallel) callers.
sealed class RateLimiter
{
    private readonly TimeSpan _minInterval;
    private DateTimeOffset _lastCall = DateTimeOffset.MinValue;

    public RateLimiter(int maxPerMinute = 18)
    {
        _minInterval = TimeSpan.FromMinutes(1.0 / maxPerMinute);
    }

    public async Task WaitAsync()
    {
        var wait = _minInterval - (DateTimeOffset.UtcNow - _lastCall);
        if (wait > TimeSpan.Zero)
            await Task.Delay(wait);
        _lastCall = DateTimeOffset.UtcNow;
    }
}
