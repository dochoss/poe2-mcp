using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Poe2Mcp.Core;
using Poe2Mcp.Core.AI;
using Xunit;

namespace Poe2Mcp.Tests.AI;

public class RecommendationEngineTests
{
    private readonly Mock<IChatClient> _mockChatClient;
    private readonly Mock<ILogger<RecommendationEngine>> _mockLogger;
    private readonly AIOptions _options;
    private readonly RecommendationEngine _engine;

    public RecommendationEngineTests()
    {
        _mockChatClient = new Mock<IChatClient>();
        _mockLogger = new Mock<ILogger<RecommendationEngine>>();
        _options = new AIOptions
        {
            ApiKey = "test-key",
            Model = "test-model",
            MaxTokens = 1024,
            Temperature = 0.7
        };

        _engine = new RecommendationEngine(
            _mockChatClient.Object,
            Options.Create(_options),
            _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new RecommendationEngine(
            _mockChatClient.Object,
            Options.Create(_options),
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new RecommendationEngine(
            _mockChatClient.Object,
            null!,
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithNullChatClient_LogsWarning()
    {
        // Act
        var engine = new RecommendationEngine(
            null,
            Options.Create(_options),
            _mockLogger.Object);

        // Assert
        engine.Should().NotBeNull();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not configured")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_WithNullCharacterData_ThrowsArgumentNullException()
    {
        // Arrange
        var analysis = new { OverallScore = 0.85 };

        // Act
        var act = async () => await _engine.GenerateRecommendationsAsync(null!, analysis);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("characterData");
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_WithNullAnalysis_ThrowsArgumentNullException()
    {
        // Arrange
        var characterData = new { Name = "TestChar" };

        // Act
        var act = async () => await _engine.GenerateRecommendationsAsync(characterData, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("analysis");
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_WithNoChatClient_ReturnsErrorMessage()
    {
        // Arrange
        var engine = new RecommendationEngine(
            null,
            Options.Create(_options),
            _mockLogger.Object);
        var characterData = new { Name = "TestChar" };
        var analysis = new { OverallScore = 0.85 };

        // Act
        var result = await engine.GenerateRecommendationsAsync(characterData, analysis);

        // Assert
        result.Should().Contain("not available");
        result.Should().Contain("not configured");
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_WithValidData_CallsChatClient()
    {
        // Arrange
        var expectedResponse = "AI recommendations";
        var mockResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, expectedResponse));
        var characterData = new { Name = "TestChar", Class = "Sorceress", Level = 50 };
        var analysis = new
        {
            OverallScore = 0.85,
            Tier = "A",
            Strengths = new[] { "High DPS", "Good defenses" },
            Weaknesses = new[] { "Low resistance" }
        };

        _mockChatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _engine.GenerateRecommendationsAsync(characterData, analysis);

        // Assert
        result.Should().Be(expectedResponse);
        _mockChatClient.Verify(
            x => x.GetResponseAsync(
                It.Is<IEnumerable<ChatMessage>>(msgs =>
                    msgs.Any(m => m.Role == ChatRole.User &&
                              m.Text!.Contains("TestChar") &&
                              m.Text.Contains("Sorceress") &&
                              m.Text.Contains("50"))),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_IncludesAnalysisData()
    {
        // Arrange
        var mockResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "recommendations"));
        var characterData = new { Name = "TestChar" };
        var analysis = new
        {
            OverallScore = 0.85,
            Tier = "A",
            Strengths = new[] { "High DPS" },
            Weaknesses = new[] { "Low resistance" }
        };

        _mockChatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        await _engine.GenerateRecommendationsAsync(characterData, analysis);

        // Assert
        _mockChatClient.Verify(
            x => x.GetResponseAsync(
                It.Is<IEnumerable<ChatMessage>>(msgs =>
                    msgs.Any(m => m.Text!.Contains("0.85") &&
                              m.Text.Contains("A") &&
                              m.Text.Contains("High DPS") &&
                              m.Text.Contains("Low resistance"))),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_WithCamelCaseProperties_WorksCorrectly()
    {
        // Arrange
        var mockResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "recommendations"));
        var characterData = new { Name = "TestChar" };
        var analysis = new
        {
            overall_score = 0.85,  // snake_case
            tier = "A",
            strengths = new[] { "High DPS" },
            weaknesses = new[] { "Low resistance" }
        };

        _mockChatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _engine.GenerateRecommendationsAsync(characterData, analysis);

        // Assert
        result.Should().Be("recommendations");
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_SetsCorrectChatOptions()
    {
        // Arrange
        var mockResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "response"));
        var characterData = new { Name = "TestChar" };
        var analysis = new { OverallScore = 0.85 };

        _mockChatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        await _engine.GenerateRecommendationsAsync(characterData, analysis);

        // Assert
        _mockChatClient.Verify(
            x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.Is<ChatOptions>(opts =>
                    opts.MaxOutputTokens == _options.MaxTokens &&
                    Math.Abs(opts.Temperature!.Value - (float)_options.Temperature) < 0.001),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_WithException_ReturnsErrorMessage()
    {
        // Arrange
        var characterData = new { Name = "TestChar" };
        var analysis = new { OverallScore = 0.85 };

        _mockChatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API Error"));

        // Act
        var result = await _engine.GenerateRecommendationsAsync(characterData, analysis);

        // Assert
        result.Should().Contain("Could not generate");
        result.Should().Contain("API Error");
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_WithCancellationToken_PassesToChatClient()
    {
        // Arrange
        var mockResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "response"));
        var characterData = new { Name = "TestChar" };
        var analysis = new { OverallScore = 0.85 };
        var cts = new CancellationTokenSource();

        _mockChatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        await _engine.GenerateRecommendationsAsync(characterData, analysis, cts.Token);

        // Assert
        _mockChatClient.Verify(
            x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_WithNullResponseText_ReturnsDefaultMessage()
    {
        // Arrange
        var mockResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, (string?)null));
        var characterData = new { Name = "TestChar" };
        var analysis = new { OverallScore = 0.85 };

        _mockChatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _engine.GenerateRecommendationsAsync(characterData, analysis);

        // Assert
        result.Should().Be("No recommendations generated.");
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_LogsInformation()
    {
        // Arrange
        var mockResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "response"));
        var characterData = new { Name = "TestChar" };
        var analysis = new { OverallScore = 0.85 };

        _mockChatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        await _engine.GenerateRecommendationsAsync(characterData, analysis);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Generating AI recommendations")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_WithMissingProperties_UsesDefaults()
    {
        // Arrange
        var mockResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "response"));
        var characterData = new { };  // Empty object
        var analysis = new { };  // Empty object

        _mockChatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _engine.GenerateRecommendationsAsync(characterData, analysis);

        // Assert
        result.Should().Be("response");
        _mockChatClient.Verify(
            x => x.GetResponseAsync(
                It.Is<IEnumerable<ChatMessage>>(msgs =>
                    msgs.Any(m => m.Text!.Contains("Unknown") ||  // Character name/class
                              m.Text.Contains("?"))),  // Level
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
