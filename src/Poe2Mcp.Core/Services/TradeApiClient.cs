using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Services;

/// <summary>
/// Official Path of Exile Trade API client
/// Searches for items on the trade market
/// </summary>
public class TradeApiClient : ITradeApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TradeApiClient> _logger;
    private readonly IRateLimiter _rateLimiter;
    private readonly TradeApiOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public TradeApiClient(
        HttpClient httpClient,
        ILogger<TradeApiClient> logger,
        IRateLimiter rateLimiter,
        IOptions<TradeApiOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Configure base URL
        if (!string.IsNullOrEmpty(_options.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        }

        // Configure headers
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:144.0) Gecko/20100101 Firefox/144.0");
        }
        if (!_httpClient.DefaultRequestHeaders.Contains("Accept"))
        {
            _httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
        }
        if (!_httpClient.DefaultRequestHeaders.Contains("X-Requested-With"))
        {
            _httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        }
        if (!_httpClient.DefaultRequestHeaders.Contains("Origin"))
        {
            _httpClient.DefaultRequestHeaders.Add("Origin", "https://www.pathofexile.com");
        }

        // Add session cookie if provided
        if (!string.IsNullOrEmpty(_options.PoeSessionId))
        {
            if (!_httpClient.DefaultRequestHeaders.Contains("Cookie"))
            {
                _httpClient.DefaultRequestHeaders.Add("Cookie", $"POESESSID={_options.PoeSessionId}");
            }
        }
        else
        {
            _logger.LogWarning(
                "No POESESSID cookie configured. Trade API searches may be limited or fail. " +
                "See appsettings.json for configuration instructions.");
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TradeItemListing>> SearchItemsAsync(
        string league,
        TradeSearchQuery query,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(league);
        ArgumentNullException.ThrowIfNull(query);
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than zero");
        }

        try
        {
            await _rateLimiter.WaitAsync("trade-api", cancellationToken);

            // Build search query
            var searchQuery = BuildSearchQuery(query);

            // Perform search - Note: /api/trade2/search/poe2/{league}
            var searchUrl = $"/api/trade2/search/poe2/{league}";

            _logger.LogInformation("Searching trade market in {League}", league);
            _logger.LogDebug("Query: {Query}", JsonSerializer.Serialize(searchQuery, _jsonOptions));

            var response = await _httpClient.PostAsJsonAsync(searchUrl, searchQuery, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var searchResult = await response.Content.ReadFromJsonAsync<JsonObject>(_jsonOptions, cancellationToken);
            var resultIds = searchResult?["result"]?.AsArray()
                .Take(limit)
                .Select(id => id?.GetValue<string>())
                .Where(id => !string.IsNullOrEmpty(id))
                .Cast<string>()
                .ToList() ?? new List<string>();

            var queryId = searchResult?["id"]?.GetValue<string>();

            if (!resultIds.Any())
            {
                _logger.LogInformation("No items found matching criteria");
                return Array.Empty<TradeItemListing>();
            }

            _logger.LogInformation("Found {Count} items, fetching details...", resultIds.Count);

            // Add delay between requests
            await Task.Delay(500, cancellationToken);

            // Fetch item details
            var items = await FetchItemDetailsAsync(resultIds, queryId, cancellationToken);

            return items;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Trade API HTTP error: {StatusCode} - {Message}",
                ex.StatusCode, ex.Message);
            return Array.Empty<TradeItemListing>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trade API error: {Message}", ex.Message);
            return Array.Empty<TradeItemListing>();
        }
    }

    private async Task<IReadOnlyList<TradeItemListing>> FetchItemDetailsAsync(
        List<string> itemIds,
        string? queryId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _rateLimiter.WaitAsync("trade-api", cancellationToken);

            // Join IDs with commas
            var idString = string.Join(",", itemIds.Take(10)); // Max 10 at a time

            // PoE2 trade API uses /api/trade2/fetch/
            var fetchUrl = $"/api/trade2/fetch/{idString}";

            // Add query parameter if we have a query_id
            if (!string.IsNullOrEmpty(queryId))
            {
                fetchUrl += $"?query={queryId}";
            }

            var response = await _httpClient.GetAsync(fetchUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<JsonObject>(_jsonOptions, cancellationToken);
            var results = data?["result"]?.AsArray() ?? new JsonArray();

            var items = new List<TradeItemListing>();

            foreach (var itemData in results)
            {
                if (itemData == null) continue;

                var item = ParseItemListing(itemData.AsObject());
                if (item != null)
                {
                    items.Add(item);
                }
            }

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching item details: {Message}", ex.Message);
            return Array.Empty<TradeItemListing>();
        }
    }

    private TradeItemListing? ParseItemListing(JsonObject rawData)
    {
        try
        {
            var listing = rawData["listing"]?.AsObject();
            var item = rawData["item"]?.AsObject();

            if (listing == null || item == null)
            {
                return null;
            }

            var price = listing["price"]?.AsObject();
            var account = listing["account"]?.AsObject();

            return new TradeItemListing
            {
                Id = rawData["id"]?.GetValue<string>() ?? "",
                Name = item["name"]?.GetValue<string>() ?? "",
                Type = item["typeLine"]?.GetValue<string>() ?? "",
                BaseType = item["baseType"]?.GetValue<string>() ?? "",
                ItemLevel = item["ilvl"]?.GetValue<int>() ?? 0,
                Corrupted = item["corrupted"]?.GetValue<bool>() ?? false,
                Price = new TradePrice
                {
                    Amount = price?["amount"]?.GetValue<double>(),
                    Currency = price?["currency"]?.GetValue<string>() ?? "",
                    Type = price?["type"]?.GetValue<string>() ?? ""
                },
                Seller = new TradeSeller
                {
                    Account = account?["name"]?.GetValue<string>() ?? "",
                    Character = account?["lastCharacterName"]?.GetValue<string>() ?? "",
                    Online = account?["online"]?.GetValue<bool>() ?? false
                },
                ExplicitMods = item["explicitMods"]?.AsArray()
                    .Select(m => m?.GetValue<string>() ?? "")
                    .ToList() ?? new List<string>(),
                ImplicitMods = item["implicitMods"]?.AsArray()
                    .Select(m => m?.GetValue<string>() ?? "")
                    .ToList() ?? new List<string>(),
                EnchantMods = item["enchantMods"]?.AsArray()
                    .Select(m => m?.GetValue<string>() ?? "")
                    .ToList() ?? new List<string>(),
                Links = CountLinks(item["sockets"]?.AsArray()),
                ListedTime = ParseDateTime(listing["indexed"]?.GetValue<string>())
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing item: {Message}", ex.Message);
            return null;
        }
    }

    private int CountLinks(JsonArray? sockets)
    {
        if (sockets == null || sockets.Count == 0)
        {
            return 0;
        }

        var maxLinks = 0;
        foreach (var socket in sockets)
        {
            var group = socket?["group"]?.GetValue<int>() ?? 0;
            if (group > maxLinks)
            {
                maxLinks = group;
            }
        }

        return maxLinks > 0 ? maxLinks + 1 : 0;
    }

    private DateTime? ParseDateTime(string? dateStr)
    {
        if (string.IsNullOrEmpty(dateStr))
        {
            return null;
        }

        if (DateTime.TryParse(dateStr, out var result))
        {
            return result;
        }

        return null;
    }

    private object BuildSearchQuery(TradeSearchQuery query)
    {
        var searchQuery = new
        {
            query = new
            {
                status = new { option = "securable" }, // PoE2 uses "securable" not "online"
                stats = new[] { new { type = "and", filters = Array.Empty<object>() } },
                term = query.Term,
                type = query.Type,
                name = query.Name,
                filters = query.ItemFilters
            },
            sort = new
            {
                price = "asc" // Cheapest first
            }
        };

        return searchQuery;
    }
}
