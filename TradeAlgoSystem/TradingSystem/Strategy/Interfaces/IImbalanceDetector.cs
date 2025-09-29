namespace TradingSystem.Strategy.Interfaces
{
    using TradingSystem.Core.Models;

    public interface IImbalanceDetector
    {
        List<ImbalanceSignal> DetectImbalances(List<TickBar> bars);
        bool CheckBearishBreakthrough(TickBar bar);
        bool CheckBullishBreakthrough(TickBar bar);
        bool CheckExtremeImbalance(TickBar bar);
        bool CheckWhaleActivity(TickBar bar);
    }
}
