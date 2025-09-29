namespace TradingSystem.Core.Models
{
    public enum SignalType
    {
        BearishBreakthrough,
        BullishBreakthrough,
        ExtremeImbalance,
        FastBreakthrough,
        WhaleActivity,
        Accumulation,
        Distribution,
        TrueBreakout,
        FalseBreakout
    }

    public enum SignalStrength
    {
        Critical,
        Strong,
        Moderate,
        Weak
    }

    public class ImbalanceSignal
    {
        public SignalType Type { get; set; }
        public SignalStrength Strength { get; set; }
        public string Description { get; set; }
        public Dictionary<string, double> Metrics { get; set; }
        public DateTime Timestamp { get; set; }
        public double ConfidenceLevel { get; set; }
    }
}
