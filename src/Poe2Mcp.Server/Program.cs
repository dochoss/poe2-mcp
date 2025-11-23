using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Poe2Mcp.Server;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", 
            optional: true, reloadOnChange: true);
        config.AddEnvironmentVariables();
        config.AddCommandLine(args);
    })
    .ConfigureServices((context, services) =>
    {
        // Register MCP server
        services.AddSingleton<McpServer>();
        
        // Register configuration sections
        services.Configure<McpServerOptions>(
            context.Configuration.GetSection("McpServer"));
        services.Configure<DatabaseOptions>(
            context.Configuration.GetSection("Database"));
        services.Configure<CacheOptions>(
            context.Configuration.GetSection("Cache"));
        
        // TODO: Register services from Poe2Mcp.Core
        // services.AddDbContext<Poe2DbContext>();
        // services.AddMemoryCache();
        // services.AddScoped<ICharacterFetcher, CharacterFetcher>();
        // etc.
    })
    .ConfigureLogging((context, logging) =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddDebug();
    });

var host = builder.Build();

// Get and run the MCP server
var mcpServer = host.Services.GetRequiredService<McpServer>();
await mcpServer.RunAsync();
