namespace Poe2Mcp.Core.Models.CurrencyExchange;

/// <summary>
/// Response from GET /currency-exchange endpoint
/// </summary>
public class CurrencyExchangeResponse
{
    /// <summary>
    /// Unix timestamp truncated to the hour for next pagination
    /// </summary>
    public uint NextChangeId { get; set; }

    /// <summary>
    /// List of market data for all active currency pairs
    /// </summary>
    public List<CurrencyExchangeMarket> Markets { get; set; } = new();
}
