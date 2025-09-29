namespace TradingSystem.Core.Models
{
    public enum BreakoutType
    {
        Bullish,
        Bearish,
        False
    }

    public enum BreakoutGrade
    {
        Moderate,
        Strong,
        Explosive
    }

    /// <summary>
    /// Інформація про пробій зони.
    /// </summary>
    public class BreakoutInfo
    {
        public BreakoutType Type { get; set; } = BreakoutType.False;
        public BreakoutGrade Grade { get; set; } = BreakoutGrade.Moderate;
        public double Strength { get; set; }            // 0..1
        public double VolumeExpansion { get; set; }     // Множник обсягу
        public double PriceExtension { get; set; }      // наскільки ціна пробила
        public bool IsConfirmed { get; set; }
    }
}
