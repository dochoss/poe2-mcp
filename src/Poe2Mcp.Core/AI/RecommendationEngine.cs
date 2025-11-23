using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Poe2Mcp.Core.AI;

/// <summary>
/// Recommendation engine interface
/// </summary>
public interface IRecommendationEngine
{
    /// <summary>
    /// Generate AI recommendations based on character analysis
    /// </summary>
    /// <param name="characterData">Character data</param>
    /// <param name="analysis">Build analysis results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AI-generated recommendations</returns>
    Task<string> GenerateRecommendationsAsync(
        object characterData,
        object analysis,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Generates AI-powered build recommendations
/// </summary>
public class RecommendationEngine : IRecommendationEngine
{
    private readonly IChatClient? _chatClient;
    private readonly ILogger<RecommendationEngine> _logger;
    private readonly AIOptions _options;

    public RecommendationEngine(
        IChatClient? chatClient,
        IOptions<AIOptions> options,
        ILogger<RecommendationEngine> logger)
    {
        _chatClient = chatClient;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        if (_chatClient == null)
        {
            _logger.LogWarning("AI chat client not configured. Recommendation engine will be limited.");
        }
    }

    public async Task<string> GenerateRecommendationsAsync(
        object characterData,
        object analysis,
        CancellationToken cancellationToken = default)
    {
        if (characterData == null)
        {
            throw new ArgumentNullException(nameof(characterData));
        }

        if (analysis == null)
        {
            throw new ArgumentNullException(nameof(analysis));
        }

        if (_chatClient == null)
        {
            return "AI recommendations not available (API key not configured)";
        }

        try
        {
            _logger.LogInformation("Generating AI recommendations for character analysis");

            // Extract character info (using reflection to handle dynamic objects)
            var characterName = GetPropertyValue(characterData, "Name") ?? "Unknown";
            var characterClass = GetPropertyValue(characterData, "Class") ?? "Unknown";
            var characterLevel = GetPropertyValue(characterData, "Level") ?? "?";

            // Extract analysis info
            var overallScore = GetPropertyValue(analysis, "OverallScore") ?? GetPropertyValue(analysis, "overall_score") ?? 0.0;
            var tier = GetPropertyValue(analysis, "Tier") ?? GetPropertyValue(analysis, "tier") ?? "Unknown";
            var strengths = GetListPropertyValue(analysis, "Strengths") ?? GetListPropertyValue(analysis, "strengths") ?? new List<string> { "None" };
            var weaknesses = GetListPropertyValue(analysis, "Weaknesses") ?? GetListPropertyValue(analysis, "weaknesses") ?? new List<string> { "None" };

            var prompt = $"""
Analyze this Path of Exile 2 character and provide specific recommendations:

Character: {characterName}
Class: {characterClass}
Level: {characterLevel}

Build Analysis:
- Overall Score: {overallScore:F2}/1.00
- Tier: {tier}
- Strengths: {string.Join(", ", strengths)}
- Weaknesses: {string.Join(", ", weaknesses)}

Provide 3-5 specific, actionable recommendations to improve this build.
""";

            _logger.LogDebug("Recommendation prompt: {Prompt}", prompt);

            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.User, prompt)
            };

            var response = await _chatClient.GetResponseAsync(
                messages,
                new ChatOptions
                {
                    MaxOutputTokens = _options.MaxTokens,
                    Temperature = (float)_options.Temperature
                },
                cancellationToken);

            var responseText = string.IsNullOrWhiteSpace(response.Text) 
                ? "No recommendations generated." 
                : response.Text;

            _logger.LogInformation("AI recommendations generated successfully. Response length: {Length} characters",
                responseText.Length);

            return responseText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI recommendations failed: {Error}", ex.Message);
            return $"Could not generate AI recommendations: {ex.Message}";
        }
    }

    private static object? GetPropertyValue(object obj, string propertyName)
    {
        var property = obj.GetType().GetProperty(propertyName);
        return property?.GetValue(obj);
    }

    private static List<string>? GetListPropertyValue(object obj, string propertyName)
    {
        var value = GetPropertyValue(obj, propertyName);
        if (value == null)
        {
            return null;
        }

        if (value is IEnumerable<string> stringList)
        {
            return stringList.ToList();
        }

        if (value is System.Collections.IEnumerable enumerable)
        {
            var result = new List<string>();
            foreach (var item in enumerable)
            {
                if (item != null)
                {
                    result.Add(item.ToString() ?? string.Empty);
                }
            }
            return result;
        }

        return null;
    }
}
