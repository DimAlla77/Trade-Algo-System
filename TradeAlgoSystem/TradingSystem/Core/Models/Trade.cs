namespace TradingSystem.Core.Models
{
    public enum TradeSide
    {
        Buy,
        Sell
    }

    public class Trade
    {
        public string Id { get; set; }
        public double Price { get; set; }
        public double Volume { get; set; }
        public TradeSide Side { get; set; }
        public DateTime Timestamp { get; set; }
        public string Exchange { get; set; }
    }
}
