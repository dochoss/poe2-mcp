using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Poe2Mcp.Core;
using Poe2Mcp.Core.Data;
using Poe2Mcp.Core.Services;
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
        // Register configuration sections
        services.Configure<McpServerOptions>(
            context.Configuration.GetSection("McpServer"));
        services.Configure<DatabaseOptions>(
            context.Configuration.GetSection("Database"));
        services.Configure<CacheOptions>(
            context.Configuration.GetSection("Cache"));
        services.Configure<RateLimitingOptions>(
            context.Configuration.GetSection("RateLimiting"));
        services.Configure<FeaturesOptions>(
            context.Configuration.GetSection("Features"));
        services.Configure<PoeApiOptions>(
            context.Configuration.GetSection("PoeApi"));
        services.Configure<PoeNinjaOptions>(
            context.Configuration.GetSection("PoeNinja"));
        services.Configure<TradeApiOptions>(
            context.Configuration.GetSection("TradeApi"));
        
        // Register DbContext
        services.AddDbContext<Poe2DbContext>((serviceProvider, options) =>
        {
            var dbOptions = context.Configuration.GetSection("Database").Get<DatabaseOptions>()
                ?? new DatabaseOptions();
            options.UseSqlite(dbOptions.ConnectionString);
            
            if (dbOptions.EnableSensitiveDataLogging)
            {
                options.EnableSensitiveDataLogging();
            }
        });
        
        // Register memory cache
        services.AddMemoryCache();
        
        // Register HttpClient factory for API clients
        services.AddHttpClient<IPoeApiClient, PoeApiClient>((serviceProvider, client) =>
        {
            var options = context.Configuration.GetSection("PoeApi").Get<PoeApiOptions>()
                ?? new PoeApiOptions();
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
        });
        
        services.AddHttpClient<IPoeNinjaApiClient, PoeNinjaApiClient>((serviceProvider, client) =>
        {
            var options = context.Configuration.GetSection("PoeNinja").Get<PoeNinjaOptions>()
                ?? new PoeNinjaOptions();
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
        });
        
        services.AddHttpClient<ITradeApiClient, TradeApiClient>((serviceProvider, client) =>
        {
            var options = context.Configuration.GetSection("TradeApi").Get<TradeApiOptions>()
                ?? new TradeApiOptions();
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
        });
        
        services.AddHttpClient<ICharacterFetcher, CharacterFetcher>((serviceProvider, client) =>
        {
            client.BaseAddress = new Uri("https://www.pathofexile.com/api");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        
        // Register core services
        services.AddSingleton<ICacheService, CacheService>();
        services.AddSingleton<IRateLimiter, RateLimiter>();
        
        // Register MCP server
        services.AddSingleton<Poe2McpServer>();
    })
    .ConfigureLogging((context, logging) =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddDebug();
    });

var host = builder.Build();

// Initialize cache service
var cacheService = host.Services.GetRequiredService<ICacheService>() as CacheService;
if (cacheService != null)
{
    await cacheService.InitializeAsync();
}

// Ensure database is created
using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<Poe2DbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

// Get and run the MCP server
var mcpServer = host.Services.GetRequiredService<Poe2McpServer>();
await mcpServer.RunAsync();
