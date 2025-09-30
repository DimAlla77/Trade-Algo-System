using TradingSystem.Core;
using TradingSystem.Strategy;
using TradingSystem.Processing;
using TradingSystem.ConsoleApp;
using TradingSystem.Exchange.Bybit;
using TradingSystem.Exchange.Binance;
using TradingSystem.Exchange.Interfaces;
using TradingSystem.Strategy.Implementation;

using static System.Console;
using TradingSystem.ConsoleApp.Models;

var _config = new Config();
ForegroundColor = ConsoleColor.Yellow;
WriteLine("Starting Trading System - Market Imbalance Detector");
WriteLine("==================================================");ResetColor();

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
tickBarBuilder.OnBarCompleted += async (sender, bar) =>
{
    marketAnalyzer.UpdateHistoricalBars(bar);
    ForegroundColor = ConsoleColor.Green;
    var line = $"[{bar.TickSize} ticks] Bar completed: " +
        $"Vol={bar.Volume:F2}, Delta={bar.DeltaPercentage:P}, " +
        $"Time={bar.FormationTime.TotalSeconds:F1}s";
    WriteLine(line);
    using(var ctx = new TradingSystem.ConsoleApp.Connection.DataContext(_config.ConnectStr))
    {
        var model = new HistoricalBar
        {
            Tick = bar.TickSize,
            Volume = bar.Volume.ToString(),
            Delta = $"{bar.DeltaPercentage:P}",
            Time = $"{bar.FormationTime.TotalSeconds:F1}s"
        };
        ctx.HistoricalBars.Add(model);
        var res = await ctx.SaveChangesAsync();
    }
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

WriteLine("\nSystem is running. Press 'Q' to quit.");

// Wait for exit
while (true)
{
    var key = ReadKey(true);
    if (key.Key == ConsoleKey.Q)
        break;
}

// Stop system
orchestrator.Stop();

WriteLine("\nSystem stopped.");