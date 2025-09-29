namespace TradingSystem.Core.Models
{
    public class MarketMetrics
    {
        public double OpenInterest { get; set; }
        public double OpenInterestChange { get; set; }
        public double FundingRate { get; set; }
        public double ATR { get; set; }
        public double VWAP { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
