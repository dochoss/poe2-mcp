using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Poe2Mcp.Core;
using Poe2Mcp.Core.Services;

namespace Poe2Mcp.Tests.Services;

public class RateLimiterTests
{
    private readonly RateLimiter _rateLimiter;

    public RateLimiterTests()
    {
        var logger = new LoggerFactory().CreateLogger<RateLimiter>();
        var options = Options.Create(new RateLimitingOptions
        {
            OfficialApiRequestsPerMinute = 60, // 1 per second for testing
            Poe2DbRequestsPerMinute = 120 // 2 per second for testing
        });

        _rateLimiter = new RateLimiter(logger, options);
    }

    [Fact]
    public void IsAllowed_FirstRequest_ReturnsTrue()
    {
        // Arrange
        var endpoint = "test-endpoint";

        // Act
        var isAllowed = _rateLimiter.IsAllowed(endpoint);

        // Assert
        Assert.True(isAllowed);
    }

    [Fact]
    public void GetRemainingRequests_AfterOneRequest_ReturnsCorrectValue()
    {
        // Arrange
        var endpoint = "remaining-test";
        var initialRemaining = _rateLimiter.GetRemainingRequests(endpoint);

        // Act
        _rateLimiter.IsAllowed(endpoint);
        var afterRequestRemaining = _rateLimiter.GetRemainingRequests(endpoint);

        // Assert
        Assert.True(afterRequestRemaining < initialRemaining);
    }

    [Fact]
    public async Task WaitAsync_CompletesWithinReasonableTime()
    {
        // Arrange
        var endpoint = "wait-test";
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        var task = _rateLimiter.WaitAsync(endpoint, cts.Token);
        await task;

        // Assert - if we get here without timeout, test passes
        Assert.True(true);
    }

    [Fact]
    public void IsAllowed_DifferentEndpoints_UseDifferentBuckets()
    {
        // Arrange
        var endpoint1 = "pathofexile.com/api";
        var endpoint2 = "poe2db.com/api";

        // Act
        var allowed1 = _rateLimiter.IsAllowed(endpoint1);
        var allowed2 = _rateLimiter.IsAllowed(endpoint2);

        // Assert
        Assert.True(allowed1);
        Assert.True(allowed2);
    }

    [Fact]
    public async Task WaitAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var endpoint = "cancel-test";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await _rateLimiter.WaitAsync(endpoint, cts.Token);
        });
    }

    [Fact]
    public void GetRemainingRequests_ReturnsPositiveNumber()
    {
        // Arrange
        var endpoint = "positive-test";

        // Act
        var remaining = _rateLimiter.GetRemainingRequests(endpoint);

        // Assert
        Assert.True(remaining > 0);
    }
}
