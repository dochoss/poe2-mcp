using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Poe2Mcp.Core;
using Poe2Mcp.Core.AI;
using Poe2Mcp.Core.Analyzers;
using Poe2Mcp.Core.Calculators;
using Poe2Mcp.Core.Data;
using Poe2Mcp.Core.Optimizers;
using Poe2Mcp.Core.Services;
using Poe2Mcp.Server;
using Poe2Mcp.Server.Tools;

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
        services.Configure<CharacterFetcherOptions>(
            context.Configuration.GetSection("CharacterFetcher"));
        services.Configure<AIOptions>(
            context.Configuration.GetSection("AI"));
        
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
        
        // Register calculators
        services.AddSingleton<IEhpCalculator, EhpCalculator>();
        services.AddSingleton<ISpiritCalculator, SpiritCalculator>();
        services.AddSingleton<IDamageCalculator, DamageCalculator>();
        services.AddSingleton<IStunCalculator, StunCalculator>();
        
        // Register analyzers
        services.AddSingleton<IWeaknessDetector, WeaknessDetector>();
        services.AddSingleton<IGearEvaluator, GearEvaluator>();
        services.AddSingleton<IBuildScorer, BuildScorer>();
        services.AddSingleton<IContentReadinessChecker, ContentReadinessChecker>();
        
        // Register optimizers
        services.AddSingleton<IGearOptimizer, GearOptimizer>();
        services.AddSingleton<IPassiveOptimizer, PassiveOptimizer>();
        services.AddSingleton<ISkillOptimizer, SkillOptimizer>();
        services.AddSingleton<IGemSynergyCalculator, GemSynergyCalculator>();
        
        // Register AI components
        services.AddSingleton<IMechanicsKnowledgeBase, PoE2MechanicsKnowledgeBase>();
        services.AddSingleton<IQueryHandler, QueryHandler>();
        services.AddSingleton<IRecommendationEngine, RecommendationEngine>();
        
        // Register MCP server with tools
        services.AddSingleton<Poe2Tools>();
        services.AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly(); // Automatically discovers tools marked with [McpServerToolType]
        
        // Register our MCP server wrapper
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

// Log MCP server startup
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("=== PoE2 Build Optimizer MCP Server ===");
logger.LogInformation("Phase 7: MCP Tools Implementation Complete");
logger.LogInformation("All 27 tools registered and ready");
logger.LogInformation("Server listening on stdio transport");

// Get and run the MCP server
var mcpServer = host.Services.GetRequiredService<Poe2McpServer>();
await mcpServer.RunAsync();
