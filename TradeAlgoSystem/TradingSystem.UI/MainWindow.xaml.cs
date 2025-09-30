namespace TradingSystem.UI
{
    using System.Windows;
    using TradingSystem.Core;
    using System.Windows.Media;
    using TradingSystem.Strategy;
    using TradingSystem.Processing;
    using System.Windows.Documents;
    using TradingSystem.UI.Connectors;
    using TradingSystem.Exchange.Bybit;
    using TradingSystem.Exchange.Binance;
    using TradingSystem.Exchange.Interfaces;
    using TradingSystem.Strategy.Implementation;

    public partial class MainWindow : Window
    {
        private string swLogPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt"),
            commonLogPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "commonLogPath.log");
        private Models.TradeModel model;
        private readonly object loglock = new object(), commonLogLock = new object();
        private readonly List<IExchangeClient> exchanges;

        private readonly TickBarBuilder tickBarBuilder;
        private readonly MarketDataAggregator dataAggregator;
        private readonly MarketAnalyzer marketAnalyzer;
        private readonly ImbalanceConfig imbalanceConfig;
        private readonly ImbalanceDetector imbalanceDetector;
        private readonly NotificationService notificationService;

        public MainWindow()
        {
            InitializeComponent();
            InitMain();

            exchanges = new List<IExchangeClient>
            {
                new BybitClient(App.Bybit_API_Key, App.Bybit_API_Secret),
                new BinanceClient(App.Binance_API_Key, App.Binance_API_Secret)
            };
            tickBarBuilder = new TickBarBuilder();
            dataAggregator = new MarketDataAggregator(tickBarBuilder);
            marketAnalyzer = new MarketAnalyzer();
            imbalanceConfig = new ImbalanceConfig();
            imbalanceDetector = new ImbalanceDetector(imbalanceConfig, marketAnalyzer);
            notificationService = new NotificationService();
            tickBarBuilder.OnBarCompleted += async (sender, bar) =>
            {
                marketAnalyzer.UpdateHistoricalBars(bar);
                var line = $"[{bar.TickSize} ticks] Bar completed: " +
                    $"Vol={bar.Volume:F2}, Delta={bar.DeltaPercentage:P}, " +
                    $"Time={bar.FormationTime.TotalSeconds:F1}s";
                model.
            };
        }

        private void InitMain()
        {
            DataContext = new Models.TradeModel(); // Ensure DataContext is set
            model = DataContext as Models.TradeModel;
            model.LogError = LogError;
            model.LogInfo = LogInfo;
            model.LogWarning = LogWarning;
            model.LogClear = LogClear;
            Model.CommonLogSave = CommonLogSave;
        }

        #region Log's methods:
        private void LogOrderSuccess(string message)
        {
            Log(message, Colors.Green, Color.FromRgb(0, 255, 0));
        }

        private void LogInfo(string message)
        {
            Log(message, Colors.White, Color.FromRgb(0x00, 0x23, 0x44));
        }

        private void LogError(string message)
        {
            Log(message, Color.FromRgb(0xf3, 0x56, 0x51), Color.FromRgb(0xf3, 0x56, 0x51));
        }

        private void LogWarning(string message)
        {
            Log(message, Colors.LightBlue, Colors.Blue);
        }

        private void LogClear()
        {
            logBlock.Text = "";
        }

        private void Log(string _message, Color color, Color dashboardColor)
        {
            string message = DateTime.Now.ToString("HH:mm:ss.ffffff") + "> " + _message + "\r\n";
            lock (loglock)
            {
                if (swLogPath != null)
                {
                    System.IO.File.AppendAllText(swLogPath, message);
                    Model.CommonLogSave(message);
                }
            }
            SafeInvoke(() =>
            {
                model.LastLog = _message;
                model.LastLogBrush = new SolidColorBrush(dashboardColor);
                Run r = new Run(message)
                {
                    Tag = DateTime.Now,
                    Foreground = new SolidColorBrush(color)
                };
                try
                {
                    while (logBlock.Inlines.Count > 250)
                    {
                        logBlock.Inlines.Remove(logBlock.Inlines.LastInline);
                    }
                }
                catch
                {

                }
                int count = logBlock.Inlines.Count;
                if (count == 0) logBlock.Inlines.Add(r);
                else
                {
                    logBlock.Inlines.InsertBefore(logBlock.Inlines.FirstInline, r);
                }
            });
        }

        public void SafeInvoke(Action action)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                if (!Model.Closing)
                {
                    action();
                }
            }));
        }

        public void CommonLogSave(string message)
        {
            lock (commonLogLock)
            {
                System.IO.File.AppendAllText(commonLogPath, message);
            }
        }
        #endregion

        private void start_btn_Click(object sender, RoutedEventArgs e)
        {
            for (var i = 0; i < 10; i++)
            {
                ///Thread.Sleep(1500);
                LogError("Test log message " + i);
            }
        }
    }
}