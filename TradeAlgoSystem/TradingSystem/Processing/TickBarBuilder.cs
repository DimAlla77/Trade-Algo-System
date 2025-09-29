namespace TradingSystem.Processing
{
    using TradingSystem.Core.Models;

    public class TickBarBuilder
    {
        private readonly Dictionary<int, TickBar> _currentBars = new();
        private readonly Dictionary<int, List<TickBar>> _completedBars = new();
        private readonly int[] _tickSizes = { 500, 1000, 5000, 15000 };
        private double _lastPrice;
        private double _currentOpenInterest;

        public TickBarBuilder()
        {
            foreach (var size in _tickSizes)
            {
                _currentBars[size] = new TickBar
                {
                    TickSize = size,
                    FormationStartTime = DateTime.UtcNow
                };
                _completedBars[size] = new List<TickBar>();
            }
        }

        public void ProcessTrade(Trade trade)
        {
            foreach (var size in _tickSizes)
            {
                UpdateTickBar(_currentBars[size], trade);

                if (_currentBars[size].TickCount >= size)
                {
                    CompleteBar(size);
                }
            }

            _lastPrice = trade.Price;
        }

        private void UpdateTickBar(TickBar bar, Trade trade)
        {
            bar.TickCount++;
            bar.Volume += trade.Volume;

            if (trade.Side == TradeSide.Sell)
                bar.AskVolume += trade.Volume;
            else
                bar.BidVolume += trade.Volume;

            if (bar.TickCount == 1)
            {
                bar.OpenPrice = trade.Price;
                bar.HighPrice = trade.Price;
                bar.LowPrice = trade.Price;
            }
            else
            {
                bar.HighPrice = Math.Max(bar.HighPrice, trade.Price);
                bar.LowPrice = Math.Min(bar.LowPrice, trade.Price);
            }

            bar.ClosePrice = trade.Price;

            // Calculate price impact
            if (_lastPrice > 0 && bar.TickCount == 1)
            {
                bar.PriceImpact = Math.Abs(trade.Price - _lastPrice);
            }
        }

        private void CompleteBar(int size)
        {
            var completedBar = _currentBars[size];
            CalculateFinalMetrics(completedBar);
            _completedBars[size].Add(completedBar);

            // Start new bar
            _currentBars[size] = new TickBar
            {
                TickSize = size,
                FormationStartTime = DateTime.UtcNow
            };

            // Trigger event for completed bar
            OnBarCompleted?.Invoke(this, completedBar);
        }

        private void CalculateFinalMetrics(TickBar bar)
        {
            // Estimate OpenPos changes based on volume distribution
            var totalVolume = bar.AskVolume + bar.BidVolume;
            if (totalVolume > 0)
            {
                if (bar.AskVolume > bar.BidVolume)
                {
                    bar.OpenPosAskChange = (_currentOpenInterest * (bar.AskVolume / totalVolume));
                }
                else
                {
                    bar.OpenPosBidChange = (_currentOpenInterest * (bar.BidVolume / totalVolume));
                }
            }
        }

        public void UpdateOpenInterest(double openInterest)
        {
            var change = openInterest - _currentOpenInterest;
            _currentOpenInterest = openInterest;

            // Distribute OI change to current bars based on their volume
            foreach (var bar in _currentBars.Values)
            {
                if (bar.AskVolume > bar.BidVolume)
                {
                    bar.OpenPosAskChange += change * (bar.AskVolume / bar.Volume);
                }
                else
                {
                    bar.OpenPosBidChange += change * (bar.BidVolume / bar.Volume);
                }
            }
        }

        public List<TickBar> GetCompletedBars(int tickSize, int count)
        {
            if (!_completedBars.ContainsKey(tickSize))
                return new List<TickBar>();

            var bars = _completedBars[tickSize];
            return bars.Skip(Math.Max(0, bars.Count - count)).ToList();
        }

        public event EventHandler<TickBar> OnBarCompleted;
    }
}
