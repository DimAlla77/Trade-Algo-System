namespace TradingSystem.Core.Models
{
    public class OrderBookSnapshot
    {
        public List<OrderBookLevel> Bids { get; set; }
        public List<OrderBookLevel> Asks { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
