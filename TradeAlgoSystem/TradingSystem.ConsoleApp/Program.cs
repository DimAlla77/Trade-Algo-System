using TradingSystem.Core;
using TradingSystem.Strategy;
using TradingSystem.Processing;
using TradingSystem.ConsoleApp;
using TradingSystem.Exchange.Bybit;
using TradingSystem.Exchange.Binance;
using TradingSystem.Exchange.Interfaces;
using TradingSystem.Strategy.Implementation;

var _config = new Config();
Console.WriteLine("Starting Trading System - Market Imbalance Detector");
Console.WriteLine("==================================================");

// Initialize components:
var exchanges = new List<IExchangeClient>
{
    new BybitClient(_config.Bybit_API_Key, _config.Bybit_API_Secret),
    new BinanceClient(_config.Binance_API_Key, _config.Binance_API_Secret)
};

var tickBarBuilder = new TickBarBuilder();
var dataAggregator = new MarketDataAggregator(tickBarBuilder);
var marketAnalyzer = new MarketAnalyzer();
var imbalanceConfig = new ImbalanceConfig();
var imbalanceDetector = new ImbalanceDetector(imbalanceConfig, marketAnalyzer);
var notificationService = new NotificationService();

// Subscribe to bar completed events
tickBarBuilder.OnBarCompleted += (sender, bar) =>
{
    marketAnalyzer.UpdateHistoricalBars(bar);
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"[{bar.TickSize} ticks] Bar completed: " +
        $"Vol={bar.Volume:F2}, Delta={bar.DeltaPercentage:P}, " +
        $"Time={bar.FormationTime.TotalSeconds:F1}s");
};

// Create orchestrator
var orchestrator = new TradingSystemOrchestrator(
    exchanges,
    dataAggregator,
    imbalanceDetector,
    marketAnalyzer,
    notificationService
);

// Start system
await orchestrator.StartAsync();

Console.WriteLine("\nSystem is running. Press 'Q' to quit.");

// Wait for exit
while (true)
{
    var key = Console.ReadKey(true);
    if (key.Key == ConsoleKey.Q)
        break;
}

// Stop system
orchestrator.Stop();

Console.WriteLine("\nSystem stopped.");