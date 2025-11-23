using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Poe2Mcp.Core;
using Poe2Mcp.Core.Models;
using Poe2Mcp.Core.Services;

namespace Poe2Mcp.Tests.Services;

public class PoeApiClientTests : IDisposable
{
    private readonly Mock<ILogger<PoeApiClient>> _mockLogger;
    private readonly Mock<ICacheService> _mockCache;
    private readonly Mock<IRateLimiter> _mockRateLimiter;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly PoeApiClient _client;

    public PoeApiClientTests()
    {
        _mockLogger = new Mock<ILogger<PoeApiClient>>();
        _mockCache = new Mock<ICacheService>();
        _mockRateLimiter = new Mock<IRateLimiter>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.pathofexile.com")
        };

        var options = Options.Create(new PoeApiOptions
        {
            BaseUrl = "https://api.pathofexile.com",
            CacheTtlSeconds = 300,
            RequestTimeoutSeconds = 30
        });

        _client = new PoeApiClient(
            _httpClient,
            _mockLogger.Object,
            _mockCache.Object,
            _mockRateLimiter.Object,
            options);
    }

    [Fact]
    public async Task GetCharacterAsync_WithValidData_ReturnsCharacter()
    {
        // Arrange
        var accountName = "TestAccount";
        var characterName = "TestChar";

        _mockCache.Setup(x => x.GetAsync<CharacterData>(
            It.IsAny<string>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((CharacterData?)null);

        _mockRateLimiter.Setup(x => x.WaitAsync(
            It.IsAny<string>(), 
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var responseData = new CharacterData
        {
            Name = characterName,
            Account = accountName,
            Level = 85,
            Class = "Warrior"
        };

        var jsonResponse = JsonSerializer.Serialize(responseData);
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _client.GetCharacterAsync(accountName, characterName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(characterName, result.Name);
        Assert.Equal(accountName, result.Account);
        Assert.Equal(85, result.Level);
        Assert.Equal("Warrior", result.Class);

        _mockCache.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<CharacterData>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCharacterAsync_WithCachedData_ReturnsCachedCharacter()
    {
        // Arrange
        var accountName = "TestAccount";
        var characterName = "TestChar";

        var cachedData = new CharacterData
        {
            Name = characterName,
            Account = accountName,
            Level = 90,
            Class = "Sorceress"
        };

        _mockCache.Setup(x => x.GetAsync<CharacterData>(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedData);

        // Act
        var result = await _client.GetCharacterAsync(accountName, characterName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(cachedData, result);

        // Verify HTTP was not called
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetCharacterAsync_WithNotFound_ReturnsNull()
    {
        // Arrange
        var accountName = "TestAccount";
        var characterName = "NonExistent";

        _mockCache.Setup(x => x.GetAsync<CharacterData>(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((CharacterData?)null);

        _mockRateLimiter.Setup(x => x.WaitAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        // Act
        var result = await _client.GetCharacterAsync(accountName, characterName);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCharacterAsync_WithForbidden_ReturnsNull()
    {
        // Arrange
        var accountName = "TestAccount";
        var characterName = "PrivateChar";

        _mockCache.Setup(x => x.GetAsync<CharacterData>(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((CharacterData?)null);

        _mockRateLimiter.Setup(x => x.WaitAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Forbidden
            });

        // Act
        var result = await _client.GetCharacterAsync(accountName, characterName);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("", "TestChar")]
    [InlineData("TestAccount", "")]
    public async Task GetCharacterAsync_WithEmptyInput_ThrowsException(string accountName, string characterName)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _client.GetCharacterAsync(accountName, characterName));
    }

    [Theory]
    [InlineData(null, "TestChar")]
    [InlineData("TestAccount", null)]
    public async Task GetCharacterAsync_WithNullInput_ThrowsException(string? accountName, string? characterName)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _client.GetCharacterAsync(accountName!, characterName!));
    }

    [Fact]
    public async Task GetPassiveTreeAsync_ReturnsTreeData()
    {
        // Arrange
        _mockCache.Setup(x => x.GetAsync<PassiveTreeData>(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((PassiveTreeData?)null);

        var responseData = new PassiveTreeData
        {
            Nodes = new List<PassiveNodeData>
            {
                new PassiveNodeData { Id = 1, Name = "Test Node", IsKeystone = true }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(responseData);
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _client.GetPassiveTreeAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Nodes);
        Assert.Equal("Test Node", result.Nodes[0].Name);

        // Verify cache was set with 24 hour TTL
        _mockCache.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<PassiveTreeData>(),
            It.Is<TimeSpan>(ts => ts.TotalHours == 24),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
