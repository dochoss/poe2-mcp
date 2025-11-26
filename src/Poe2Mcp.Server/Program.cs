using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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

var useStreamableHttp = HostApplicationBuilderExtensions.UseStreamableHttp(
  Environment.GetEnvironmentVariables(), 
  args);

// FOR DEV
useStreamableHttp = true;

IHostApplicationBuilder builder = useStreamableHttp
                                ? WebApplication.CreateBuilder(args)
                                : Host.CreateApplicationBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (useStreamableHttp == true)
{
  var port = "20002";
  (builder as WebApplicationBuilder)!.WebHost.UseUrls($"http://localhost:{port}");

  Console.WriteLine($"Listening on http://localhost:{port}/poe2mcp");
}


// Configure configuration sources
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

#region Services
// Configure options
builder.Services.Configure<McpServerOptions>(builder.Configuration.GetSection("McpServer"));
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));
builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection("Cache"));
builder.Services.Configure<RateLimitingOptions>(builder.Configuration.GetSection("RateLimiting"));
builder.Services.Configure<FeaturesOptions>(builder.Configuration.GetSection("Features"));
builder.Services.Configure<PoeApiOptions>(builder.Configuration.GetSection("PoeApi"));
builder.Services.Configure<PoeNinjaOptions>(builder.Configuration.GetSection("PoeNinja"));
builder.Services.Configure<TradeApiOptions>(builder.Configuration.GetSection("TradeApi"));
builder.Services.Configure<CharacterFetcherOptions>(builder.Configuration.GetSection("CharacterFetcher"));
builder.Services.Configure<AIOptions>(builder.Configuration.GetSection("AI"));

// Register DbContext
builder.Services.AddDbContext<Poe2DbContext>((serviceProvider, options) =>
{
    var dbOptions = builder.Configuration.GetSection("Database").Get<DatabaseOptions>()
        ?? new DatabaseOptions();
    options.UseSqlite(dbOptions.ConnectionString);

    if (dbOptions.EnableSensitiveDataLogging)
    {
        options.EnableSensitiveDataLogging();
    }
});

// Register memory cache
builder.Services.AddMemoryCache();

// Register HttpClient factory for API clients
builder.Services.AddHttpClient<IPoeApiClient, PoeApiClient>((serviceProvider, client) =>
{
    var options = builder.Configuration.GetSection("PoeApi").Get<PoeApiOptions>()
        ?? new PoeApiOptions();
    client.BaseAddress = new Uri(options.BaseUrl);
    client.DefaultRequestHeaders.Add("User-Agent", options.UserAgent);
  client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
});

builder.Services.AddHttpClient<IPoeNinjaApiClient, PoeNinjaApiClient>((serviceProvider, client) =>
{
    var options = builder.Configuration.GetSection("PoeNinja").Get<PoeNinjaOptions>()
        ?? new PoeNinjaOptions();
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
});

builder.Services.AddHttpClient<ITradeApiClient, TradeApiClient>((serviceProvider, client) =>
{
    var options = builder.Configuration.GetSection("TradeApi").Get<TradeApiOptions>()
        ?? new TradeApiOptions();
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
});

// CharacterFetcher no longer needs its own HttpClient - it uses IPoeApiClient
builder.Services.AddSingleton<ICharacterFetcher, CharacterFetcher>();

// Register core services
builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddSingleton<IRateLimiter, RateLimiter>();

// Register calculators
builder.Services.AddSingleton<IEhpCalculator, EhpCalculator>();
builder.Services.AddSingleton<ISpiritCalculator, SpiritCalculator>();
builder.Services.AddSingleton<IDamageCalculator, DamageCalculator>();
builder.Services.AddSingleton<IStunCalculator, StunCalculator>();

// Register analyzers
builder.Services.AddSingleton<IWeaknessDetector, WeaknessDetector>();
builder.Services.AddSingleton<IGearEvaluator, GearEvaluator>();
builder.Services.AddSingleton<IBuildScorer, BuildScorer>();
builder.Services.AddSingleton<IContentReadinessChecker, ContentReadinessChecker>();

// Register optimizers
builder.Services.AddSingleton<IGearOptimizer, GearOptimizer>();
builder.Services.AddSingleton<IPassiveOptimizer, PassiveOptimizer>();
builder.Services.AddSingleton<ISkillOptimizer, SkillOptimizer>();
builder.Services.AddSingleton<IGemSynergyCalculator, GemSynergyCalculator>();

// Register AI components
builder.Services.AddSingleton<IMechanicsKnowledgeBase, PoE2MechanicsKnowledgeBase>();
builder.Services.AddAiChatClient(builder.Configuration);
builder.Services.AddSingleton<IQueryHandler, QueryHandler>();
builder.Services.AddSingleton<IRecommendationEngine, RecommendationEngine>();
#endregion

var host = builder.BuildApp(useStreamableHttp);

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
logger.LogInformation("Server listening on {0} transport", (useStreamableHttp ? "HTTP" : "stdio"));

await host.RunAsync();
