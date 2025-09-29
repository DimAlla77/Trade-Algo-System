namespace TradingSystem.Strategy.Interfaces
{
    using TradingSystem.Core.Models;

    public interface IMarketAnalyzer
    {
        double GetAverageVolume(int periods);
        double GetATR();
        List<AccumulationZone> DetectAccumulationZones(List<TickBar> bars);
        bool IsBreakout(TickBar currentBar, AccumulationZone zone);
        BreakoutInfo AnalyzeBreakout(List<TickBar> bars, AccumulationZone zone);
    }
}
