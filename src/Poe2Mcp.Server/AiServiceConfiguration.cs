using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using OpenAI;
using Poe2Mcp.Core;
using System.ClientModel;

namespace Poe2Mcp.Server;

/// <summary>
/// Configuration for AI services
/// Currently supports Ollama (local development)
/// </summary>
/// <remarks>
/// To use Azure OpenAI or OpenAI in production:
/// 1. Install Microsoft.Extensions.AI.OpenAI NuGet package (preview)
/// 2. Follow examples at https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/build-chat-app
/// </remarks>
public static class AiServiceConfiguration
{
    /// <summary>
    /// Register IChatClient based on configuration
    /// Currently supports: Ollama (local development)
    /// </summary>
    public static IServiceCollection AddAiChatClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IChatClient>(serviceProvider =>
        {
            var options = configuration.GetSection("AI").Get<AIOptions>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            return CreateLocalChatClient(options, logger);
            //return CreateChatClient(options, logger);
        });

        return services;
    }

    private static IChatClient CreateLocalChatClient(AIOptions? options, ILogger<Program> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        string model = options.Model;
        string key = options.ApiKey ?? string.Empty;
        string endpoint = options.Endpoint ?? string.Empty;
        logger.LogInformation("Configuring local chat client with model: {Model}, endpoint: {Endpoint}",
            model, endpoint);
        IChatClient chatClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions
        {
            Endpoint = new Uri(endpoint)
        }).GetChatClient(model).AsIChatClient();
        return chatClient;
    }

    private static IChatClient? CreateChatClient(AIOptions? options, ILogger logger)
    {
        if (options == null || string.IsNullOrWhiteSpace(options.Endpoint))
        {
            logger.LogWarning("AI endpoint not configured. AI features will be disabled.");
            logger.LogInformation("To enable AI features, set AI:Endpoint in appsettings.json");
            logger.LogInformation("Example for Ollama: http://localhost:11434");
            return null;
        }

        try
        {
            // Ollama (local development) - OllamaApiClient implements IChatClient
            if (IsLocalEndpoint(options.Endpoint))
            {
                return CreateOllamaClient(options, logger);
            }
            // Azure OpenAI and other providers require additional packages
            else
            {
                logger.LogWarning("Only local Ollama is currently supported.");
                logger.LogWarning("For Azure OpenAI or OpenAI support:");
                logger.LogWarning("  1. Install Microsoft.Extensions.AI.OpenAI NuGet package");
                logger.LogWarning("  2. See https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/build-chat-app");
                return null;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to configure AI chat client: {Error}", ex.Message);
            return null;
        }
    }

    private static IChatClient CreateOllamaClient(AIOptions options, ILogger logger)
    {
        logger.LogInformation("Configuring Ollama client: {Endpoint}, Model: {Model}",
            options.Endpoint, options.Model);
        logger.LogInformation("Make sure Ollama is running with: ollama serve");
        logger.LogInformation("Pull the model with: ollama pull {Model}", options.Model);

        // OllamaApiClient implements IChatClient directly (requires explicit cast)
        return (IChatClient)new OllamaApiClient(new Uri(options.Endpoint), options.Model);
    }

    private static bool IsLocalEndpoint(string endpoint)
    {
        return endpoint.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
               endpoint.Contains("127.0.0.1", StringComparison.Ordinal) ||
               endpoint.Contains("0.0.0.0", StringComparison.Ordinal);
    }
}
