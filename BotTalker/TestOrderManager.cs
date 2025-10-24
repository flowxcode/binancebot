using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Enums;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.Logging;

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
                type: SpotOrderType.LimitMaker,
                quantity: 0.001m // ~108 USD | 10% Alloc on 1k€
                ,
                price: 109580m
            );
            if (testBuyResult.Success)
            {
                _logger.LogInformation($"✅ Test BUY Executed (Mock): Qty: 0.001 BTC (No {testBuyResult.Data.Id} for Test)");
                //_logger.LogInformation($" Est Entry: ~108.50 USDT | RR: 1:2 | +2.17€ TP / -1.08€ SL (1% Risk)");
            }
            else
            {
                _logger.LogError($"❌ Test BUY Fail: {testBuyResult.Error?.Message} | Verify Trading Perms + IP Whitelist");
            }
            Console.WriteLine();

            //// Test Sell: Limit TP @ +1% | Maker Fee Edge
            //var testSellResult = await client.SpotApi.Trading.PlaceOrderAsync(
            //    symbol: "BTCUSDT",
            //    side: OrderSide.Sell,
            //    type: SpotOrderType.Market,
            //    //timeInForce: TimeInForce.GoodTillCanceled,
            //    quantity: 0.001m
            ////price: 109580m // +1% Target
            //);
            //if (testSellResult.Success)
            //{
            //    _logger.LogInformation($"✅ Test SELL Queued (Mock): TP @ 109,580 USDT (No {testSellResult.Data.Id} for Test)");
            //    //_logger.LogInformation($" Fees Est: 0.2% RT = -0.22€ | Net: +1.95€ (Spot) | Vol Edge: 1.65% Daily Avg");
            //}
            //else
            //{
            //    _logger.LogError($"❌ Test SELL Fail: {testSellResult.Error?.Message} | Verify Trading Perms + IP Whitelist");
            //}
            //Console.WriteLine();

            // Fetch: GET /api/v3/openOrders (Weight: 3, up to 500 last)
            var openOrdersResult = await client.SpotApi.Trading.GetOpenOrdersAsync(symbol: "BTCUSDT");  // Or null for all symbols

            if (openOrdersResult.Success)
            {
                var orders = openOrdersResult.Data;
                if (orders.ToList().Count == 0)
                {
                    _logger.LogInformation("✅ No Open Orders: Clean Slate | Ready for Next Signal");
                }
                else
                {
                    _logger.LogInformation($"📋 Found {orders.ToList().Count} Open Orders:");
                    foreach (var order in orders)
                    {
                        _logger.LogInformation($"  - Order ID: {order.Id} | Side: {order.Side} | Type: {order.Type}");
                        _logger.LogInformation($"    Qty: {order.Quantity} BTC | Price: {order.Price} USDT | Status: {order.Status}");
                        _logger.LogInformation($"    Time: {order.CreateTime:yyyy-MM-dd HH:mm:ss} UTC | Est Fill: ~{order.QuantityFilled} BTC");
                        // Sim P&L: Add custom calc if needed, e.g., RR 1:2 on 1% Risk
                        Console.WriteLine();
                    }
                }
            }
            else
            {
                _logger.LogError($"❌ Fetch Fail: {openOrdersResult.Error?.Message} | Check Key/Perms or IP Whitelist");
            }

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

    public async Task PlaceFuturesOrdersAsync()
    {
        Console.WriteLine();
        //_logger.LogInformation("🧪 Firing Test Orders: BTC/USDT Spot (Mock—Docs: /api/v3/order/test)");

        var client = new BinanceRestClient(options =>
        {
            options.Environment = BinanceEnvironment.Demo;
            options.ApiCredentials = new ApiCredentials(_apiKey, _apiSecret);
        });

        try
        {
            // Define allocation percentage (e.g., 10% of available USDT as margin)
            decimal allocationPercentage = 0.10m;  // 10%
            //decimal allocationPercentage = 1;  // 10%
            int leverage = 10;

            // First, set leverage if not already
            var leverageResponse = await client.UsdFuturesApi.Account.ChangeInitialLeverageAsync(
                symbol: "BTCUSDT",
                leverage: leverage
            );

            if (!leverageResponse.Success)
            {
                // Handle error
            }

            // Fetch futures account balances
            var balancesResponse = await client.UsdFuturesApi.Account.GetBalancesAsync();

            if (!balancesResponse.Success)
            {
                // Handle error
            }

            var usdtBalance = balancesResponse.Data.FirstOrDefault(b => b.Asset == "USDT");

            if (usdtBalance == null || usdtBalance.AvailableBalance <= 0)
            {
                // No USDT available; handle
                return;
            }

            decimal availableUsdt = usdtBalance.AvailableBalance;
            decimal desiredMargin = availableUsdt * allocationPercentage;
            decimal quoteQuantity = desiredMargin * leverage;  // Notional in USDT

            // Fetch current price for BTCUSDT
            var priceResponse = await client.UsdFuturesApi.ExchangeData.GetPriceAsync("BTCUSDT");
            if (!priceResponse.Success)
            {
                // Handle error, e.g., Console.WriteLine(priceResponse.Error.Message);
                return;
            }
            decimal currentPrice = priceResponse.Data.Price;

            var quantityBtc = quoteQuantity / currentPrice;

            // Round quantity to the symbol's stepSize precision (e.g., 0.001 for BTCUSDT; fetch via GetExchangeInfoAsync if needed)
            quantityBtc = Math.Floor(quantityBtc * 1000) / 1000; // Example: floor to 0.001 BTC

            // Ensure minimum quantity (e.g., 0.001 BTC; adjust per symbol rules)
            if (quantityBtc < 0.001m)
            {
                // Handle: too small
                quantityBtc = 0.001m;
            }

            Console.WriteLine($"try availableUsdt: {availableUsdt} desiredMargin: {desiredMargin} quantityBtc: {quantityBtc}");

            // Place the long market order using quoteQuantity
            var orderResponse = await client.UsdFuturesApi.Trading.PlaceOrderAsync(
                symbol: "BTCUSDT",
                side: OrderSide.Buy,
                type: FuturesOrderType.Market,
                quantity: quantityBtc
            );

            if (orderResponse.Success)
            {
                // Order placed; check orderResponse.Data for details
                _logger.LogInformation($"✅ Long Executed {orderResponse.Data.Id}");
            }
            else
            {
                _logger.LogError($"❌ Long Fail: {orderResponse.Error?.Message}");
            }

            //var longOrder = await client.UsdFuturesApi.Trading.PlaceOrderAsync(
            //    symbol: "BTCUSDT",
            //    side: OrderSide.Buy,
            //    type: SpotOrderType.LimitMaker,
            //    quantity: 0.001m // ~108 USD | 10% Alloc on 1k€
            //    ,
            //    price: 109580m
            //);
            //if (testBuyResult.Success)
            //{
            //    _logger.LogInformation($"✅ Long Executed (Mock): Qty: 0.001 BTC (No {longOrder.Data.Id} for Test)");
            //    //_logger.LogInformation($" Est Entry: ~108.50 USDT | RR: 1:2 | +2.17€ TP / -1.08€ SL (1% Risk)");
            //}
            //else
            //{
            //    _logger.LogError($"❌ Test BUY Fail: {longOrder.Error?.Message} | Verify Trading Perms + IP Whitelist");
            //}

            _logger.LogInformation("📈 Test Suite Done..");
        }
        catch (Exception ex)
        {
            _logger.LogError($"💥 Order Exception: {ex.Message} | Check Demo Balance");
        }
        finally
        {
            client.Dispose();
        }
    }
}