namespace TradingSystem.Core.Models
{
    public class AccumulationZone
    {
        public double PriceLevel { get; set; }
        public double VolumeAccumulated { get; set; }
        public TimeSpan Duration { get; set; }
        public double OpenPosImbalance { get; set; }
        public ZoneType Type { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int TouchCount { get; set; }
        public double StrengthScore { get; set; }
    }

    public enum ZoneType
    {
        Support,
        Resistance,
        Accumulation,
        Distribution
    }
}
