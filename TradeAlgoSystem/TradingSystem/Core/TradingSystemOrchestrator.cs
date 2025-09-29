namespace TradingSystem.Core
{
    using TradingSystem.Core.Models;
    using TradingSystem.Exchange.Interfaces;
    using TradingSystem.Strategy.Interfaces;

    public class TradingSystemOrchestrator
    {
        private readonly List<IExchangeClient> _exchanges;
        private readonly IDataAggregator _dataAggregator;
        private readonly IImbalanceDetector _imbalanceDetector;
        private readonly IMarketAnalyzer _marketAnalyzer;
        private readonly INotificationService _notificationService;
        private readonly Timer _analysisTimer;
        private readonly SemaphoreSlim _analysisSemaphore = new(1, 1);

        public TradingSystemOrchestrator(
            List<IExchangeClient> exchanges,
            IDataAggregator dataAggregator,
            IImbalanceDetector imbalanceDetector,
            IMarketAnalyzer marketAnalyzer,
            INotificationService notificationService)
        {
            _exchanges = exchanges;
            _dataAggregator = dataAggregator;
            _imbalanceDetector = imbalanceDetector;
            _marketAnalyzer = marketAnalyzer;
            _notificationService = notificationService;

            // Run analysis every 100ms
            _analysisTimer = new Timer(async _ => await RunAnalysisAsync(), null,
                TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));
        }

        public async Task StartAsync()
        {
            // Connect to all exchanges
            var connectTasks = _exchanges.Select(e => ConnectExchange(e));
            await Task.WhenAll(connectTasks);

            Console.WriteLine($"Connected to {_exchanges.Count} exchanges");
        }

        private async Task ConnectExchange(IExchangeClient exchange)
        {
            try
            {
                await exchange.ConnectAsync();

                // Subscribe to data streams
                await exchange.SubscribeToTradesAsync("ETHUSDT");
                await exchange.SubscribeToOrderBookAsync("ETHUSDT");
                await exchange.SubscribeToOpenInterestAsync("ETHUSDT");

                // Hook up event handlers
                exchange.OnTradeReceived += (s, trade) => _dataAggregator.ProcessTrade(trade);
                exchange.OnOrderBookUpdate += (s, book) => _dataAggregator.UpdateOrderBook(book);
                exchange.OnOpenInterestUpdate += (s, oi) => _dataAggregator.UpdateOpenInterest(oi, exchange.ExchangeName);

                Console.WriteLine($"Connected to {exchange.ExchangeName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to {exchange.ExchangeName}: {ex.Message}");
            }
        }

        private async Task RunAnalysisAsync()
        {
            if (!await _analysisSemaphore.WaitAsync(0))
                return; // Skip if previous analysis still running

            try
            {
                // Get tick bars for different sizes
                var tickSizes = new[] { 500, 1000, 5000, 15000 };
                var allSignals = new List<ImbalanceSignal>();

                foreach (var size in tickSizes)
                {
                    var bars = _dataAggregator.GetTickBars(size, 50);
                    if (bars.Count == 0) continue;

                    // Detect imbalances
                    var signals = _imbalanceDetector.DetectImbalances(bars);
                    allSignals.AddRange(signals);

                    // Detect accumulation zones
                    var zones = _marketAnalyzer.DetectAccumulationZones(bars);

                    // Check for breakouts
                    foreach (var zone in zones)
                    {
                        if (_marketAnalyzer.IsBreakout(bars.LastOrDefault(), zone))
                        {
                            var breakoutInfo = _marketAnalyzer.AnalyzeBreakout(bars, zone);
                            allSignals.Add(CreateBreakoutSignal(breakoutInfo, zone));
                        }
                    }
                }

                // Process signals
                await ProcessSignalsAsync(allSignals);
            }
            finally
            {
                _analysisSemaphore.Release();
            }
        }

        private async Task ProcessSignalsAsync(List<ImbalanceSignal> signals)
        {
            // Group by strength
            var criticalSignals = signals.Where(s => s.Strength == SignalStrength.Critical);
            var strongSignals = signals.Where(s => s.Strength == SignalStrength.Strong);

            // Send notifications for critical signals
            foreach (var signal in criticalSignals)
            {
                await _notificationService.SendAlertAsync(signal);
            }

            // Queue strong signals for validation
            foreach (var signal in strongSignals)
            {
                await _notificationService.QueueForValidationAsync(signal);
            }

            // Log moderate signals
            var moderateSignals = signals.Where(s => s.Strength == SignalStrength.Moderate);
            foreach (var signal in moderateSignals)
            {
                Console.WriteLine($"[{signal.Timestamp:HH:mm:ss}] {signal.Type}: {signal.Description}");
            }
        }

        private ImbalanceSignal CreateBreakoutSignal(BreakoutInfo breakout, AccumulationZone zone)
        {
            var signalType = breakout.Type == BreakoutType.Bullish
                ? SignalType.BullishBreakthrough
                : SignalType.BearishBreakthrough;

            if (breakout.Type == BreakoutType.False)
                signalType = SignalType.FalseBreakout;

            var strength = breakout.Grade switch
            {
                BreakoutGrade.Explosive => SignalStrength.Critical,
                BreakoutGrade.Strong => SignalStrength.Strong,
                _ => SignalStrength.Moderate
            };

            return new ImbalanceSignal
            {
                Type = signalType,
                Strength = strength,
                Description = $"Breakout from {zone.Type} at {zone.PriceLevel:F2}",
                Metrics = new Dictionary<string, double>
                {
                    ["ZoneStrength"] = zone.StrengthScore,
                    ["VolumeExpansion"] = breakout.VolumeExpansion,
                    ["PriceExtension"] = breakout.PriceExtension
                },
                Timestamp = DateTime.UtcNow,
                ConfidenceLevel = breakout.Strength
            };
        }

        public void Stop()
        {
            _analysisTimer?.Dispose();

            foreach (var exchange in _exchanges)
            {
                exchange.Disconnect();
            }
        }
    }

    public interface INotificationService
    {
        Task SendAlertAsync(ImbalanceSignal signal);
        Task QueueForValidationAsync(ImbalanceSignal signal);
    }
}






   // OLDEST:
   //public class TradingSystemOrchestrator
   // {
   //     private readonly BybitClient _bybitClient;
   //     private readonly BinanceClient _binanceClient;
   //     private readonly MarketAnalyzer _marketAnalyzer;
   //     private readonly NotificationService _notificationService;
   //     private readonly Dictionary<string, BarBuilder> _barBuilders = new();
   //     private readonly CancellationTokenSource _cts = new();

   //     public TradingSystemOrchestrator(
   //         BybitClient bybitClient,
   //         BinanceClient binanceClient,
   //         MarketAnalyzer marketAnalyzer,
   //         NotificationService notificationService)
   //     {
   //         _bybitClient = bybitClient;
   //         _binanceClient = binanceClient;
   //         _marketAnalyzer = marketAnalyzer;
   //         _notificationService = notificationService;
   //     }

   //     public async Task StartAsync(IEnumerable<string> symbols)
   //     {
   //         Console.WriteLine("🚀 Trading system starting...");

   //         // підписка на трейди і книги ордерів
   //         foreach (var symbol in symbols)
   //         {
   //             _barBuilders[symbol] = new BarBuilder(symbol, TimeSpan.FromMinutes(1));

   //             await _bybitClient.SubscribeToTradesAsync(symbol);
   //             await _bybitClient.SubscribeToOrderBookAsync(symbol);

   //             await _binanceClient.SubscribeToTradesAsync(symbol);
   //             await _binanceClient.SubscribeToOrderBookAsync(symbol);
   //         }

   //         // запуск циклу аналізу
   //         _ = Task.Run(() => AnalysisLoop(_cts.Token));
   //     }

   //     public void Stop()
   //     {
   //         Console.WriteLine("🛑 Stopping trading system...");
   //         _cts.Cancel();
   //         _bybitClient.Dispose();
   //         _binanceClient.Dispose();
   //     }

   //     private async Task AnalysisLoop(CancellationToken token)
   //     {
   //         while (!token.IsCancellationRequested)
   //         {
   //             try
   //             {
   //                 foreach (var kvp in _barBuilders)
   //                 {
   //                     var symbol = kvp.Key;
   //                     var builder = kvp.Value;

   //                     var bars = builder.GetCompletedBars();
   //                     if (bars.Count == 0)
   //                         continue;

   //                     var zones = _marketAnalyzer.DetectAccumulationZones(bars); // DetectImbalanceZones(bars);
   //                     var breakouts = _marketAnalyzer.DetectBreakouts(bars);

   //                     foreach (var zone in zones)
   //                     {
   //                         var message = $"[ZONE] {symbol}: {zone.ZoneType} at {zone.PriceLevel} | Strength: {zone.Strength}";
   //                         await _notificationService.SendAsync(message);
   //                     }

   //                     foreach (var breakout in breakouts)
   //                     {
   //                         var message = $"[BREAKOUT] {symbol}: {breakout.Direction} at {breakout.Price} | Type: {breakout.Type}";
   //                         await _notificationService.SendAsync(message);
   //                     }
   //                 }
   //             }
   //             catch (Exception ex)
   //             {
   //                 Console.WriteLine($"⚠️ Analysis error: {ex.Message}");
   //             }

   //             await Task.Delay(1000, token); // аналіз раз на секунду
   //         }
   //     }
   // }