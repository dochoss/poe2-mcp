using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Poe2Mcp.Server.Tools;
using Xunit;
using FluentAssertions;

namespace Poe2Mcp.Tests.Integration;

/// <summary>
/// Integration tests for MCP server and tool registration
/// </summary>
public class McpServerIntegrationTests
{
    [Fact]
    public void McpServer_Should_RegisterAllTools()
    {
        // Arrange
        var host = CreateTestHost();

        // Act
        var toolsService = host.Services.GetService<Poe2Tools>();

        // Assert
        toolsService.Should().NotBeNull("Poe2Tools should be registered in DI container");
    }

    [Fact]
    public void Poe2Tools_Should_HaveCorrectAttributes()
    {
        // Arrange
        var toolsType = typeof(Poe2Tools);

        // Act
        var typeAttribute = toolsType.GetCustomAttributes(typeof(ModelContextProtocol.Server.McpServerToolTypeAttribute), false);

        // Assert
        typeAttribute.Should().NotBeEmpty("Poe2Tools should have McpServerToolType attribute");
    }

    [Fact]
    public void Poe2Tools_Should_Have27ToolMethods()
    {
        // Arrange
        var toolsType = typeof(Poe2Tools);

        // Act
        var toolMethods = toolsType.GetMethods()
            .Where(m => m.GetCustomAttributes(typeof(ModelContextProtocol.Server.McpServerToolAttribute), false).Any())
            .ToList();

        // Assert
        toolMethods.Should().HaveCount(27, "All 27 tools should be defined");
        
        // Verify specific tools exist
        var toolNames = toolMethods
            .SelectMany(m => m.GetCustomAttributes(typeof(ModelContextProtocol.Server.McpServerToolAttribute), false)
                .Cast<ModelContextProtocol.Server.McpServerToolAttribute>())
            .Select(attr => attr.Name)
            .ToList();

        toolNames.Should().Contain("analyze_character");
        toolNames.Should().Contain("calculate_character_ehp");
        toolNames.Should().Contain("optimize_gear");
        toolNames.Should().Contain("natural_language_query");
        toolNames.Should().Contain("health_check");
    }

    [Fact]
    public async Task Poe2Tools_HealthCheck_Should_ReturnSuccess()
    {
        // Arrange
        var host = CreateTestHost();
        var tools = host.Services.GetRequiredService<Poe2Tools>();

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
                
                // Register tools
                services.AddSingleton<Poe2Tools>();
            })
            .Build();
    }
}
