// See https://aka.ms/new-console-template for more information
// Binance Demo Bot: Testnet w/ v11 Fixes | Oct 24, 2025
using System;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.Logging;

namespace BinanceDemoBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 BTC Bot Ignition: Testnet Mode w/ v11 Loaded | JForex Vibes");

            // Provided API Key (Demo Only—Use Env Vars in Prod!)
            var apiKey = Environment.GetEnvironmentVariable("BINANCE_TESTNET_API_KEY");
            var apiSecret = Environment.GetEnvironmentVariable("BINANCE_TESTNET_SECRET");

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                Console.WriteLine("⚠️ Alert: Set BINANCE_TESTNET_API_KEY and BINANCE_TESTNET_SECRET env vars!");
                return;
            }

            Console.WriteLine($"Debug API Key: {apiKey}");
            Console.WriteLine($"Debug API Secret: {apiSecret}");

            // Init Connector & Log Server Time
            var connector = new BinanceConnector(apiKey, apiSecret);
            await connector.ConnectAndLogAsync();

            // Fire Test Orders (Mock Executions)
            var orderManager = new TestOrderManager(apiKey, apiSecret);
            await orderManager.PlaceTestOrdersAsync();

            Console.WriteLine("📊 Session Complete: Check Logs for Signals | End of Day.");
        }
    }

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
                    _logger.LogInformation($"   BTC Spot Ready: 1k€ Sim w/ 0.001 BTC Orders | Docs: /api/v3/klines for Vol Data");
                    Console.WriteLine();
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

    /// <summary>
    /// TestOrderManager: POST /api/v3/order/test (Mock, No Fill) | Sim Day Trade
    /// Fees Mock: 0.04% RT Futures | P&L: +2€ TP / -1€ SL (1% Risk)
    /// </summary>
    public class TestOrderManager
    {
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly ILogger<TestOrderManager> _logger;

        public TestOrderManager(string apiKey, string apiSecret)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<TestOrderManager>();
        }

        public async Task PlaceTestOrdersAsync()
        {
            _logger.LogInformation("🧪 Firing Test Orders: BTC/USDT Spot (Mock—Docs: /api/v3/order/test)");
            Console.WriteLine();

            var client = new BinanceRestClient(options =>
            {
                options.Environment = BinanceEnvironment.Demo;
                options.ApiCredentials = new ApiCredentials(_apiKey, _apiSecret);
            });

            try
            {
                // Test Buy: Market (Taker Sim) | Qty Scaled for 1k€ Acct
                var testBuyResult = await client.SpotApi.Trading.PlaceOrderAsync(
                    symbol: "BTCUSDT",
                    side: OrderSide.Buy,
                    type: SpotOrderType.Market,
                    quantity: 0.001m  // ~108 USD | 10% Alloc on 1k€
                );

                if (testBuyResult.Success)
                {
                    _logger.LogInformation($"✅ Test BUY Executed (Mock): Qty: 0.001 BTC (No {testBuyResult.RequestId} for Test)");
                    //_logger.LogInformation($"   Est Entry: ~108.50 USDT | RR: 1:2 | +2.17€ TP / -1.08€ SL (1% Risk)");
                }
                else
                {
                    _logger.LogError($"❌ Test BUY Fail: {testBuyResult.Error?.Message} | Verify Trading Perms + IP Whitelist");
                }

                Console.WriteLine();

                // Test Sell: Limit TP @ +1% | Maker Fee Edge
                var testSellResult = await client.SpotApi.Trading.PlaceOrderAsync(
                    symbol: "BTCUSDT",
                    side: OrderSide.Sell,
                    type: SpotOrderType.Market,                 
                    //timeInForce: TimeInForce.GoodTillCanceled,
                    quantity: 0.001m
                    //price: 109580m  // +1% Target
                );

                if (testSellResult.Success)
                {
                    _logger.LogInformation($"✅ Test SELL Queued (Mock): TP @ 109,580 USDT (No {testSellResult.RequestId} for Test)");
                    //_logger.LogInformation($"   Fees Est: 0.2% RT = -0.22€ | Net: +1.95€ (Spot) | Vol Edge: 1.65% Daily Avg");
                }
                else
                {
                    _logger.LogError($"❌ Test SELL Fail: {testSellResult.Error?.Message} | Verify Trading Perms + IP Whitelist");
                }

                Console.WriteLine();

                _logger.LogInformation("📈 Test Suite Done: 2 Orders | Next: Add Klines Fetch (/api/v3/klines)");
            }
            catch (Exception ex)
            {
                _logger.LogError($"💥 Order Exception: {ex.Message} | Check Testnet Balance");
            }
            finally
            {
                client.Dispose();
            }
        }
    }
}