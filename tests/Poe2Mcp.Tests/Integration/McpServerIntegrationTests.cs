using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Poe2Mcp.Server.Tools;
using Xunit;
using FluentAssertions;

namespace Poe2Mcp.Tests.Integration;

/// <summary>
/// Integration tests for MCP server and tool registration
/// </summary>
public class McpServerIntegrationTests
{
    private static readonly Type McpServerToolTypeAttribute = typeof(McpServerToolTypeAttribute);
    private static readonly Type McpServerToolAttribute = typeof(McpServerToolAttribute);

    [Fact]
    public void McpServer_Should_RegisterAllToolServices()
    {
        // Arrange
        var host = CreateTestHost();

        // Act & Assert - Check that all tool service classes are registered
        var serverTools = host.Services.GetService<ServerTools>();
        var characterTools = host.Services.GetService<CharacterTools>();
        var analyzerTools = host.Services.GetService<AnalyzerTools>();
        var calculatorTools = host.Services.GetService<CalculatorTools>();
        var optimizerTools = host.Services.GetService<OptimizerTools>();
        var aiTools = host.Services.GetService<AITools>();
        var utilityTools = host.Services.GetService<UtilityTools>();

        serverTools.Should().NotBeNull("ServerTools should be registered in DI container");
        characterTools.Should().NotBeNull("CharacterTools should be registered in DI container");
        analyzerTools.Should().NotBeNull("AnalyzerTools should be registered in DI container");
        calculatorTools.Should().NotBeNull("CalculatorTools should be registered in DI container");
        optimizerTools.Should().NotBeNull("OptimizerTools should be registered in DI container");
        aiTools.Should().NotBeNull("AITools should be registered in DI container");
        utilityTools.Should().NotBeNull("UtilityTools should be registered in DI container");
    }

    [Fact]
    public void AllToolClasses_Should_HaveCorrectAttributes()
    {
        // Arrange
        var toolTypes = new[]
        {
            typeof(ServerTools),
            typeof(CharacterTools),
            typeof(AnalyzerTools),
            typeof(CalculatorTools),
            typeof(OptimizerTools),
            typeof(AITools),
            typeof(UtilityTools)
        };

        // Act & Assert
        foreach (var toolType in toolTypes)
        {
            var typeAttribute = toolType.GetCustomAttributes(McpServerToolTypeAttribute, false);
            typeAttribute.Should().NotBeEmpty($"{toolType.Name} should have McpServerToolType attribute");
        }
    }

    [Fact]
    public void AllToolClasses_Should_Have27ToolMethods()
    {
        // Arrange
        var toolTypes = new[]
        {
            typeof(ServerTools),
            typeof(CharacterTools),
            typeof(AnalyzerTools),
            typeof(CalculatorTools),
            typeof(OptimizerTools),
            typeof(AITools),
            typeof(UtilityTools)
        };

        // Act - Collect all tool methods from all classes
        var allToolMethods = toolTypes
            .SelectMany(type => type.GetMethods()
                .Where(m => m.GetCustomAttributes(McpServerToolAttribute, false).Any()))
            .ToList();

        // Assert
        allToolMethods.Should().HaveCount(27, "All 27 tools should be defined across all tool classes");
        
        // Verify specific tools exist
        var toolNames = allToolMethods
            .SelectMany(m => m.GetCustomAttributes(McpServerToolAttribute, false)
                .Cast<McpServerToolAttribute>())
            .Select(attr => attr.Name)
            .ToList();

        // Check for key tools from different classes
        toolNames.Should().Contain("health_check"); // ServerTools
        toolNames.Should().Contain("analyze_character"); // CharacterTools
        toolNames.Should().Contain("calculate_character_ehp"); // CalculatorTools
        toolNames.Should().Contain("optimize_gear"); // OptimizerTools
        toolNames.Should().Contain("natural_language_query"); // AITools
        toolNames.Should().Contain("detect_character_weaknesses"); // AnalyzerTools
        toolNames.Should().Contain("search_trade_items"); // UtilityTools
    }

    [Fact]
    public async Task ServerTools_HealthCheck_Should_ReturnSuccess()
    {
        // Arrange
        var host = CreateTestHost();
        var tools = host.Services.GetRequiredService<ServerTools>();

        // Act
        var result = await tools.HealthCheckAsync();

        // Assert
        result.Should().NotBeNull();
        
        // Use reflection to check the anonymous type properties
        var successProp = result.GetType().GetProperty("success");
        var statusProp = result.GetType().GetProperty("status");
        
        successProp.Should().NotBeNull();
        statusProp.Should().NotBeNull();
        
        var successValue = successProp!.GetValue(result);
        var statusValue = statusProp!.GetValue(result);
        
        successValue.Should().Be(true);
        statusValue.Should().Be("healthy");
    }

    private static IHost CreateTestHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Register minimal services needed for testing
                services.AddLogging(builder => builder.AddConsole());
                
                // Register all tool services
                services.AddSingleton<ServerTools>();
                services.AddSingleton<CharacterTools>();
                services.AddSingleton<AnalyzerTools>();
                services.AddSingleton<CalculatorTools>();
                services.AddSingleton<OptimizerTools>();
                services.AddSingleton<AITools>();
                services.AddSingleton<UtilityTools>();
            })
            .Build();
    }
}
