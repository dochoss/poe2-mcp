using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Poe2Mcp.Core;

namespace Poe2Mcp.Core.Services;

/// <summary>
/// Token bucket rate limiter implementation
/// </summary>
public class RateLimiter : IRateLimiter
{
    private readonly ILogger<RateLimiter> _logger;
    private readonly RateLimitingOptions _options;
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets = new();

    public RateLimiter(
        ILogger<RateLimiter> logger,
        IOptions<RateLimitingOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task WaitAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        var bucket = GetOrCreateBucket(endpoint);
        await bucket.WaitForTokenAsync(cancellationToken);
    }

    public bool IsAllowed(string endpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        var bucket = GetOrCreateBucket(endpoint);
        return bucket.TryConsumeToken();
    }

    public int GetRemainingRequests(string endpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        var bucket = GetOrCreateBucket(endpoint);
        return bucket.AvailableTokens;
    }

    private TokenBucket GetOrCreateBucket(string endpoint)
    {
        return _buckets.GetOrAdd(endpoint, key =>
        {
            var requestsPerMinute = GetRequestsPerMinute(key);
            _logger.LogDebug("Creating rate limit bucket for {Endpoint} with {RequestsPerMinute} req/min", 
                key, requestsPerMinute);
            return new TokenBucket(requestsPerMinute, _logger);
        });
    }

    private int GetRequestsPerMinute(string endpoint)
    {
        // Map endpoints to their rate limits
        return endpoint.ToLowerInvariant() switch
        {
            var e when e.Contains("official") || e.Contains("pathofexile.com") 
                => _options.OfficialApiRequestsPerMinute,
            var e when e.Contains("poe2db") || e.Contains("poedb") 
                => _options.Poe2DbRequestsPerMinute,
            _ => _options.OfficialApiRequestsPerMinute // Default to most restrictive
        };
    }

    /// <summary>
    /// Token bucket for rate limiting
    /// </summary>
    private class TokenBucket
    {
        private readonly int _capacity;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private double _tokens;
        private DateTime _lastRefill;

        public TokenBucket(int requestsPerMinute, ILogger logger)
        {
            _capacity = requestsPerMinute;
            _tokens = requestsPerMinute;
            _lastRefill = DateTime.UtcNow;
            _logger = logger;
        }

        public int AvailableTokens
        {
            get
            {
                RefillTokens();
                return (int)Math.Floor(_tokens);
            }
        }

        public async Task WaitForTokenAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await _semaphore.WaitAsync(cancellationToken);
                try
                {
                    RefillTokens();
                    
                    if (_tokens >= 1.0)
                    {
                        _tokens -= 1.0;
                        return;
                    }

                    // Calculate wait time until next token
                    var refillRate = _capacity / 60.0; // tokens per second
                    var tokensNeeded = 1.0 - _tokens;
                    var waitSeconds = tokensNeeded / refillRate;
                    var waitTime = TimeSpan.FromSeconds(Math.Ceiling(waitSeconds));

                    _logger.LogDebug("Rate limit reached, waiting {WaitTime}ms", waitTime.TotalMilliseconds);
                }
                finally
                {
                    _semaphore.Release();
                }

                // Wait outside the semaphore
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        public bool TryConsumeToken()
        {
            _semaphore.Wait();
            try
            {
                RefillTokens();
                
                if (_tokens >= 1.0)
                {
                    _tokens -= 1.0;
                    return true;
                }
                
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void RefillTokens()
        {
            var now = DateTime.UtcNow;
            var elapsed = now - _lastRefill;
            
            if (elapsed.TotalSeconds > 0)
            {
                var refillRate = _capacity / 60.0; // tokens per second
                var tokensToAdd = elapsed.TotalSeconds * refillRate;
                _tokens = Math.Min(_capacity, _tokens + tokensToAdd);
                _lastRefill = now;
            }
        }
    }
}
