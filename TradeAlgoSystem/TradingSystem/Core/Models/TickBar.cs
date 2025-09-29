namespace TradingSystem.Core.Models
{
    public class TickBar
    {
        public int TickSize { get; set; }
        public int TickCount { get; set; }
        public double Volume { get; set; }
        public double AskVolume { get; set; }
        public double BidVolume { get; set; }
        public double OpenPrice { get; set; }
        public double HighPrice { get; set; }
        public double LowPrice { get; set; }
        public double ClosePrice { get; set; }
        public DateTime FormationStartTime { get; set; }
        public TimeSpan FormationTime => DateTime.UtcNow - FormationStartTime;
        public double OpenPosAskChange { get; set; }
        public double OpenPosBidChange { get; set; }
        public double PriceImpact { get; set; }
        public double Delta => BidVolume - AskVolume;
        public double DeltaPercentage => Volume > 0 ? Delta / Volume : 0;
        public double PriceRange => HighPrice - LowPrice;
    }
}
