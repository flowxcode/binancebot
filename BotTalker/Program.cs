// See https://aka.ms/new-console-template for more information
// Binance Demo Bot: Demo w/ v11 Fixes | Oct 24, 2025
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace BinanceDemoBot
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var timestamp = DateTime.Now;

            // Initialize logger inside Main (static context)
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                // Use AddSimpleConsole directly for single-line formatting
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true; // Optional: Keeps category and event ID
                    options.SingleLine = true;    // Ensures single-line output
                    options.TimestampFormat = "HH:mm:ss "; // Optional: Add timestamp if desired
                });
            });
            var logger = loggerFactory.CreateLogger<Program>();

            Console.WriteLine("🚀 BTC Bot Ignition: Demo Mode w/ v11 Loaded | JForex Vibes");

            // Provided API Key (Demo Only—Use Env Vars in Prod!)
            var apiKey = Environment.GetEnvironmentVariable("BINANCE_DEMO_API_KEY");
            var apiSecret = Environment.GetEnvironmentVariable("BINANCE_DEMO_SECRET");

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                logger.LogError("⚠️ Alert: Set BINANCE_DEMO_API_KEY and BINANCE_DEMO_SECRET env vars!");
                return;
            }

            logger.LogInformation($"✅ Keys active");

            //Console.WriteLine($"Debug API Key: {apiKey}");
            //Console.WriteLine($"Debug API Secret: {apiSecret}");

            // Init Connector & Log Server Time
            var connector = new BinanceConnector(apiKey, apiSecret);
            //await connector.ConnectAndLogAsync();

            // Fire Demo Orders
            var orderManager = new TestOrderManager(apiKey, apiSecret);
            //await orderManager.PlaceTestOrdersAsync();

            // Fire Demo Orders
            await orderManager.PlaceFuturesOrdersAsync();

            logger.LogInformation("📊 Session Complete: Check Logs for Signals | End of Day.");
            var dur = DateTime.Now - timestamp;
            Console.WriteLine(dur.ToString());
        }
    }
}