using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Poe2Mcp.Core;
using Poe2Mcp.Core.Models;
using Poe2Mcp.Core.Models.Profile;
using Poe2Mcp.Core.Models.League;
using Poe2Mcp.Core.Models.CurrencyExchange;
using Poe2Mcp.Core.Models.ItemFilter;
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

        var characterData = new CharacterData
        {
            Name = characterName,
            Account = accountName,
            Level = 85,
            Class = "Warrior"
        };

        // API now returns: { "character": CharacterData }
        var responseData = new CharacterResponse
        {
            Character = characterData
        };

        var jsonResponse = JsonSerializer.Serialize(responseData, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

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
            .ReturnsAsync(() => new HttpResponseMessage
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
            .ReturnsAsync(() => new HttpResponseMessage
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
    public async Task GetProfileAsync_ReturnsProfileData()
    {
        // Arrange
        _mockCache.Setup(x => x.GetAsync<ProfileData>(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProfileData?)null);

        _mockRateLimiter.Setup(x => x.WaitAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var responseData = new ProfileData
        {
            Uuid = "550e8400-e29b-41d4-a716-446655440000",
            Name = "TestAccount",
            Locale = "en_US"
        };

        var jsonResponse = JsonSerializer.Serialize(responseData, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _client.GetProfileAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestAccount", result.Name);
        Assert.Equal("550e8400-e29b-41d4-a716-446655440000", result.Uuid);

        // Verify cache was set
        _mockCache.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<ProfileData>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListLeaguesAsync_ReturnsLeagues()
    {
        // Arrange
        _mockCache.Setup(x => x.GetAsync<List<League>>(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<League>?)null);

        _mockRateLimiter.Setup(x => x.WaitAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var responseData = new LeaguesResponse
        {
            Leagues = new List<League>
            {
                new League { Id = "Standard", Name = "Standard", Realm = "pc" },
                new League { Id = "Hardcore", Name = "Hardcore", Realm = "pc" }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(responseData, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _client.ListLeaguesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Standard", result[0].Id);
        Assert.Equal("Hardcore", result[1].Id);

        // Verify cache was set
        _mockCache.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<List<League>>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLeagueAsync_ReturnsLeague()
    {
        // Arrange
        var leagueName = "Standard";

        _mockCache.Setup(x => x.GetAsync<League>(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((League?)null);

        _mockRateLimiter.Setup(x => x.WaitAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var responseData = new LeagueResponse
        {
            League = new League
            {
                Id = leagueName,
                Name = "Standard",
                Realm = "pc"
            }
        };

        var jsonResponse = JsonSerializer.Serialize(responseData, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _client.GetLeagueAsync(leagueName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Standard", result.Id);
        Assert.Equal("Standard", result.Name);

        // Verify cache was set
        _mockCache.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<League>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCurrencyExchangeMarketsAsync_ReturnsMarketData()
    {
        // Arrange
        _mockCache.Setup(x => x.GetAsync<CurrencyExchangeResponse>(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((CurrencyExchangeResponse?)null);

        _mockRateLimiter.Setup(x => x.WaitAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var responseData = new CurrencyExchangeResponse
        {
            NextChangeId = 1234567890,
            Markets = new List<CurrencyExchangeMarket>
            {
                new CurrencyExchangeMarket
                {
                    League = "Standard",
                    MarketId = "chaos|divine",
                    VolumeTrad = new Dictionary<string, uint> { { "chaos", 1000 }, { "divine", 100 } }
                }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(responseData, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _client.GetCurrencyExchangeMarketsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1234567890u, result.NextChangeId);
        Assert.Single(result.Markets);
        Assert.Equal("chaos|divine", result.Markets[0].MarketId);

        // Verify cache was set
        _mockCache.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<CurrencyExchangeResponse>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListItemFiltersAsync_ReturnsFilters()
    {
        // Arrange
        _mockCache.Setup(x => x.GetAsync<List<ItemFilter>>(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<ItemFilter>?)null);

        _mockRateLimiter.Setup(x => x.WaitAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var responseData = new ItemFiltersResponse
        {
            Filters = new List<ItemFilter>
            {
                new ItemFilter
                {
                    Id = "filter1",
                    FilterName = "My Filter",
                    Realm = "poe2",
                    Type = "Normal"
                }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(responseData, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _client.ListItemFiltersAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("My Filter", result[0].FilterName);

        // Verify cache was set
        _mockCache.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<List<ItemFilter>>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetItemFilterAsync_ReturnsFilter()
    {
        // Arrange
        var filterId = "filter123";

        _mockCache.Setup(x => x.GetAsync<ItemFilter>(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((ItemFilter?)null);

        _mockRateLimiter.Setup(x => x.WaitAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var responseData = new ItemFilterResponse
        {
            Filter = new ItemFilter
            {
                Id = filterId,
                FilterName = "My Detailed Filter",
                Realm = "poe2",
                Filter = "Show\n ItemLevel >= 75"
            }
        };

        var jsonResponse = JsonSerializer.Serialize(responseData, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _client.GetItemFilterAsync(filterId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(filterId, result.Id);
        Assert.NotNull(result.Filter);
        Assert.Contains("ItemLevel", result.Filter);

        // Verify cache was set
        _mockCache.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<ItemFilter>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateItemFilterAsync_ReturnsCreatedFilter()
    {
        // Arrange
        _mockRateLimiter.Setup(x => x.WaitAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = new CreateItemFilterRequest
        {
            FilterName = "New Filter",
            Realm = "poe2",
            Filter = "Show\n ItemLevel >= 80"
        };

        var responseData = new ItemFilterResponse
        {
            Filter = new ItemFilter
            {
                Id = "newfilter123",
                FilterName = request.FilterName,
                Realm = request.Realm,
                Filter = request.Filter
            }
        };

        var jsonResponse = JsonSerializer.Serialize(responseData, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _client.CreateItemFilterAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("newfilter123", result.Id);
        Assert.Equal("New Filter", result.FilterName);
    }

    [Fact]
    public async Task UpdateItemFilterAsync_ReturnsUpdatedFilter()
    {
        // Arrange
        var filterId = "filter123";

        _mockRateLimiter.Setup(x => x.WaitAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = new UpdateItemFilterRequest
        {
            FilterName = "Updated Filter Name"
        };

        var responseData = new ItemFilterResponse
        {
            Filter = new ItemFilter
            {
                Id = filterId,
                FilterName = "Updated Filter Name",
                Realm = "poe2"
            }
        };

        var jsonResponse = JsonSerializer.Serialize(responseData, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _client.UpdateItemFilterAsync(filterId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(filterId, result.Id);
        Assert.Equal("Updated Filter Name", result.FilterName);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

