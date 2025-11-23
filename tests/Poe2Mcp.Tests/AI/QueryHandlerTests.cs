using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Poe2Mcp.Core;
using Poe2Mcp.Core.AI;
using Xunit;

namespace Poe2Mcp.Tests.AI;

public class QueryHandlerTests
{
    private readonly Mock<IChatClient> _mockChatClient;
    private readonly Mock<ILogger<QueryHandler>> _mockLogger;
    private readonly AIOptions _options;
    private readonly QueryHandler _queryHandler;

    public QueryHandlerTests()
    {
        _mockChatClient = new Mock<IChatClient>();
        _mockLogger = new Mock<ILogger<QueryHandler>>();
        _options = new AIOptions
        {
            ApiKey = "test-key",
            Model = "test-model",
            MaxTokens = 1024,
            Temperature = 0.7
        };

        _queryHandler = new QueryHandler(
            _mockChatClient.Object,
            Options.Create(_options),
            _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new QueryHandler(
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
        var act = () => new QueryHandler(
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
        var handler = new QueryHandler(
            null,
            Options.Create(_options),
            _mockLogger.Object);

        // Assert
        handler.Should().NotBeNull();
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
    public async Task HandleQueryAsync_WithNullQuery_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _queryHandler.HandleQueryAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("query");
    }

    [Fact]
    public async Task HandleQueryAsync_WithEmptyQuery_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _queryHandler.HandleQueryAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("query");
    }

    [Fact]
    public async Task HandleQueryAsync_WithWhitespaceQuery_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _queryHandler.HandleQueryAsync("   ");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("query");
    }

    [Fact]
    public async Task HandleQueryAsync_WithNoChatClient_ReturnsErrorMessage()
    {
        // Arrange
        var handler = new QueryHandler(
            null,
            Options.Create(_options),
            _mockLogger.Object);

        // Act
        var result = await handler.HandleQueryAsync("test query");

        // Assert
        result.Should().Contain("not enabled");
        result.Should().Contain("API key");
    }

    [Fact]
    public async Task HandleQueryAsync_WithValidQuery_CallsChatClient()
    {
        // Arrange
        var expectedResponse = "AI response text";
        var mockResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, expectedResponse));
        
        _mockChatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _queryHandler.HandleQueryAsync("What is freeze?");

        // Assert
        result.Should().Be(expectedResponse);
        _mockChatClient.Verify(
            x => x.GetResponseAsync(
                It.Is<IEnumerable<ChatMessage>>(msgs => 
                    msgs.Any(m => m.Role == ChatRole.User && m.Text!.Contains("What is freeze?"))),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleQueryAsync_WithCharacterContext_IncludesContextInMessage()
    {
        // Arrange
        var expectedResponse = "AI response with context";
        var mockResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, expectedResponse));
        var characterContext = new { Name = "TestChar", Level = 50 };
        
        _mockChatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _queryHandler.HandleQueryAsync("Analyze my character", characterContext);

        // Assert
        result.Should().Be(expectedResponse);
        _mockChatClient.Verify(
            x => x.GetResponseAsync(
                It.Is<IEnumerable<ChatMessage>>(msgs => 
                    msgs.Any(m => m.Role == ChatRole.User && 
                              m.Text!.Contains("Analyze my character") &&
                              m.Text.Contains("Character Context"))),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleQueryAsync_SetsCorrectChatOptions()
    {
        // Arrange
        var mockResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "response"));
        
        _mockChatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        await _queryHandler.HandleQueryAsync("test query");

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
    public async Task HandleQueryAsync_WithSystemPrompt_IncludesPrompt()
    {
        // Arrange
        var mockResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "response"));
        
        _mockChatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        await _queryHandler.HandleQueryAsync("test query");

        // Assert
        _mockChatClient.Verify(
            x => x.GetResponseAsync(
                It.Is<IEnumerable<ChatMessage>>(msgs => 
                    msgs.Any(m => m.Role == ChatRole.System && 
                              m.Text!.Contains("Path of Exile 2 build advisor"))),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleQueryAsync_WithException_ReturnsErrorMessage()
    {
        // Arrange
        _mockChatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API Error"));

        // Act
        var result = await _queryHandler.HandleQueryAsync("test query");

        // Assert
        result.Should().Contain("error");
        result.Should().Contain("API Error");
    }

    [Fact]
    public async Task HandleQueryAsync_WithCancellationToken_PassesToChatClient()
    {
        // Arrange
        var mockResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "response"));
        var cts = new CancellationTokenSource();
        
        _mockChatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        await _queryHandler.HandleQueryAsync("test query", null, cts.Token);

        // Assert
        _mockChatClient.Verify(
            x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task HandleQueryAsync_WithNullResponseText_ReturnsDefaultMessage()
    {
        // Arrange - Create a ChatMessage with null text
        var mockResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, (string?)null));
        
        _mockChatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _queryHandler.HandleQueryAsync("test query");

        // Assert
        result.Should().Be("No response generated.");
    }

    [Fact]
    public async Task HandleQueryAsync_LogsInformation()
    {
        // Arrange
        var mockResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "response"));
        
        _mockChatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        await _queryHandler.HandleQueryAsync("test query");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handling AI query")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
