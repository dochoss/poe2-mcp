using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Poe2Mcp.Core.AI;

/// <summary>
/// Query handler interface
/// </summary>
public interface IQueryHandler
{
    /// <summary>
    /// Handle a natural language query
    /// </summary>
    /// <param name="query">User's question</param>
    /// <param name="characterContext">Optional character data for context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AI-generated response</returns>
    Task<string> HandleQueryAsync(
        string query,
        object? characterContext = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Handles natural language queries about builds using AI
/// </summary>
public class QueryHandler : IQueryHandler
{
    private readonly IChatClient? _chatClient;
    private readonly ILogger<QueryHandler> _logger;
    private readonly AIOptions _options;

    public QueryHandler(
        IChatClient? chatClient,
        IOptions<AIOptions> options,
        ILogger<QueryHandler> logger)
    {
        _chatClient = chatClient;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        if (_chatClient == null)
        {
            _logger.LogWarning("AI chat client not configured. AI features will be limited.");
        }
    }

    public async Task<string> HandleQueryAsync(
        string query,
        object? characterContext = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be null or whitespace.", nameof(query));
        }

        if (_chatClient == null)
        {
            return "AI features are not enabled. Please set the AI API key in your configuration.";
        }

        try
        {
            _logger.LogInformation("Handling AI query: {Query}", query);

            // Build the system prompt
            var systemPrompt = """
You are an expert Path of Exile 2 build advisor.
Help players optimize their builds by providing actionable, specific advice.
Focus on practical recommendations that can be implemented immediately.
""";

            // Build user message
            var userMessage = query;
            if (characterContext != null)
            {
                userMessage += $"\n\nCharacter Context:\n{characterContext}";
            }

            // Create chat messages
            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.System, systemPrompt),
                new ChatMessage(ChatRole.User, userMessage)
            };

            // Call the AI chat client
            var response = await _chatClient.GetResponseAsync(
                messages,
                new ChatOptions
                {
                    MaxOutputTokens = _options.MaxTokens,
                    Temperature = (float)_options.Temperature
                },
                cancellationToken);

            var responseText = string.IsNullOrWhiteSpace(response.Text) 
                ? "No response generated." 
                : response.Text;

            _logger.LogInformation("AI query handled successfully. Response length: {Length} characters", 
                responseText.Length);

            return responseText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI query failed: {Error}", ex.Message);
            return $"I encountered an error processing your query: {ex.Message}";
        }
    }
}
