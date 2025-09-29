namespace TradingSystem.Exchange.Interfaces
{
    using TradingSystem.Core.Models;

    public interface IDataAggregator
    {
        void ProcessTrade(Trade trade);
        void UpdateOrderBook(OrderBookSnapshot snapshot);
        void UpdateOpenInterest(double openInterest, string exchange);
        MarketMetrics GetCurrentMetrics();
        List<TickBar> GetTickBars(int tickSize, int count);
    }
}
