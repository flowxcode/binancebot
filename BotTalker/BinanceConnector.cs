
using Binance.Net;
using Binance.Net.Clients;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.Logging;

/// <summary>
/// BinanceConnector: Hooks to Testnet API (Spot v3), Pings Server Time, Logs Status
/// Latency Check: Offset <50ms = Green for 10x Lev Trades
/// </summary>
public class BinanceConnector
{
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private readonly ILogger<BinanceConnector> _logger;

    public BinanceConnector(string apiKey, string apiSecret)
    {
        _apiKey = apiKey;
        _apiSecret = apiSecret;
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<BinanceConnector>();
    }

    public async Task ConnectAndLogAsync()
    {
        _logger.LogInformation("🔌 Connecting to Binance Demo (Spot API v3)...");

        var client = new BinanceRestClient(options =>
        {
            options.Environment = BinanceEnvironment.Demo;
            options.ApiCredentials = new ApiCredentials(_apiKey, _apiSecret);
        });

        try
        {
            // Ping: GET /api/v3/time (Low-Weight, Docs: Exchange Info)
            var timeResult = await client.SpotApi.ExchangeData.GetServerTimeAsync();
            if (timeResult.Success)
            {
                var serverTime = timeResult.Data;
                var localTime = DateTime.UtcNow;
                var offsetMs = (long)(serverTime - localTime).TotalMilliseconds;

                _logger.LogInformation($"✅ Connected! Server Time: {serverTime:yyyy-MM-dd HH:mm:ss} UTC");
                _logger.LogInformation($"   Local Time: {localTime:yyyy-MM-dd HH:mm:ss} UTC | Offset: {offsetMs}ms (Sync OK if <50ms)");
            }
            else
            {
                _logger.LogError($"❌ Connection Fail: {timeResult.Error?.Message} | Check Key/Secret Pair");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"💥 Exception: {ex.Message}");
        }
        finally
        {
            client.Dispose();
        }
    }
}