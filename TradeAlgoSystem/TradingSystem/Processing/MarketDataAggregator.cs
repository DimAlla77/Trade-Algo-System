namespace TradingSystem.Processing
{
    using TradingSystem.Core.Models;
    using TradingSystem.Exchange.Interfaces;

    public class MarketDataAggregator : IDataAggregator
    {
        private readonly TickBarBuilder _barBuilder;
        private readonly Queue<Trade> _recentTrades = new();
        private readonly Dictionary<string, double> _openInterestByExchange = new();
        private OrderBookSnapshot _latestOrderBook;
        private MarketMetrics _currentMetrics = new();
        private readonly int _maxTradeHistory = 10000;

        public MarketDataAggregator(TickBarBuilder barBuilder)
        {
            _barBuilder = barBuilder;
        }

        public void ProcessTrade(Trade trade)
        {
            _recentTrades.Enqueue(trade);
            if (_recentTrades.Count > _maxTradeHistory)
                _recentTrades.Dequeue();

            _barBuilder.ProcessTrade(trade);
            UpdateMetrics();
        }

        public void UpdateOrderBook(OrderBookSnapshot snapshot)
        {
            _latestOrderBook = snapshot;
            UpdateMetrics();
        }

        public void UpdateOpenInterest(double openInterest, string exchange)
        {
            _openInterestByExchange[exchange] = openInterest;
            _currentMetrics.OpenInterest = _openInterestByExchange.Values.Average();
            _barBuilder.UpdateOpenInterest(_currentMetrics.OpenInterest);
        }

        public MarketMetrics GetCurrentMetrics()
        {
            return _currentMetrics;
        }

        public List<TickBar> GetTickBars(int tickSize, int count)
        {
            return _barBuilder.GetCompletedBars(tickSize, count);
        }

        private void UpdateMetrics()
        {
            // Update VWAP
            if (_recentTrades.Any())
            {
                var vwapSum = _recentTrades.Sum(t => t.Price * t.Volume);
                var volumeSum = _recentTrades.Sum(t => t.Volume);
                _currentMetrics.VWAP = volumeSum > 0 ? vwapSum / volumeSum : 0;
            }

            // Update ATR (simplified)
            var bars = _barBuilder.GetCompletedBars(1000, 14);
            if (bars.Count >= 14)
            {
                _currentMetrics.ATR = bars.Average(b => b.PriceRange);
            }

            _currentMetrics.Timestamp = DateTime.UtcNow;
        }
    }
}

