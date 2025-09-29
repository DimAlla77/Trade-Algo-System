namespace TradingSystem.Strategy
{
    using TradingSystem.Core.Models;
    using TradingSystem.Strategy.Interfaces;

    public class ImbalanceDetector : IImbalanceDetector
    {
        private readonly ImbalanceConfig _config;
        private readonly IMarketAnalyzer _analyzer;

        public ImbalanceDetector(ImbalanceConfig config, IMarketAnalyzer analyzer)
        {
            _config = config;
            _analyzer = analyzer;
        }

        public List<ImbalanceSignal> DetectImbalances(List<TickBar> bars)
        {
            var signals = new List<ImbalanceSignal>();

            foreach (var bar in bars)
            {
                // Bearish Breakthrough Detection
                if (CheckBearishBreakthrough(bar))
                {
                    signals.Add(CreateSignal(SignalType.BearishBreakthrough, bar));
                }

                // Bullish Breakthrough Detection
                if (CheckBullishBreakthrough(bar))
                {
                    signals.Add(CreateSignal(SignalType.BullishBreakthrough, bar));
                }

                // Extreme Imbalance Detection
                if (CheckExtremeImbalance(bar))
                {
                    signals.Add(CreateSignal(SignalType.ExtremeImbalance, bar));
                }

                // Whale Activity Detection
                if (CheckWhaleActivity(bar))
                {
                    signals.Add(CreateSignal(SignalType.WhaleActivity, bar));
                }
            }

            return signals;
        }

        public bool CheckBearishBreakthrough(TickBar bar)
        {
            return bar.OpenPosAskChange > bar.OpenPosBidChange * _config.BearishMultiplier
                && bar.DeltaPercentage < -_config.DeltaThreshold
                && bar.AskVolume > bar.BidVolume * _config.VolumeRatio
                && bar.TickCount < _config.FastTickThreshold
                && bar.OpenPosAskChange > _config.MinOpenPosChange
                && bar.Volume > _analyzer.GetAverageVolume(10) * _config.VolumeMultiplier
                && bar.PriceRange > _config.MinPriceMovement;
        }

        public bool CheckBullishBreakthrough(TickBar bar)
        {
            return bar.OpenPosBidChange > bar.OpenPosAskChange * _config.BullishMultiplier
                && bar.DeltaPercentage > _config.DeltaThreshold
                && bar.BidVolume > bar.AskVolume * _config.VolumeRatio
                && bar.TickCount < _config.FastTickThreshold
                && bar.OpenPosBidChange > _config.MinOpenPosChange
                && bar.Volume > _analyzer.GetAverageVolume(10) * _config.VolumeMultiplier
                && bar.PriceRange > _config.MinPriceMovement;
        }

        public bool CheckExtremeImbalance(TickBar bar)
        {
            var imbalance = Math.Abs(bar.OpenPosAskChange - bar.OpenPosBidChange);
            return imbalance > _config.ExtremeImbalanceThreshold
                || Math.Abs(bar.DeltaPercentage) > _config.ExtremeDeltaThreshold
                || (bar.AskVolume / bar.BidVolume > _config.ExtremeRatioThreshold ||
                    bar.BidVolume / bar.AskVolume > _config.ExtremeRatioThreshold);
        }

        public bool CheckWhaleActivity(TickBar bar)
        {
            return bar.TickCount <= _config.WhaleTickThreshold
                && bar.Volume > _config.WhaleVolumeThreshold
                && Math.Abs(bar.OpenPosAskChange - bar.OpenPosBidChange) > _config.WhaleImbalanceThreshold
                && bar.PriceImpact > _config.WhalePriceImpact
                && bar.FormationTime.TotalSeconds < _config.WhaleTimeThreshold;
        }

        private ImbalanceSignal CreateSignal(SignalType type, TickBar bar)
        {
            return new ImbalanceSignal
            {
                Type = type,
                Strength = CalculateStrength(type, bar),
                Description = GenerateDescription(type, bar),
                Metrics = ExtractMetrics(bar),
                Timestamp = DateTime.UtcNow,
                ConfidenceLevel = CalculateConfidence(type, bar)
            };
        }

        private SignalStrength CalculateStrength(SignalType type, TickBar bar)
        {
            // Implementation of strength calculation logic
            var score = 0m;

            // Add scoring logic based on various factors
            if (bar.Volume > _analyzer.GetAverageVolume(20) * 3) score += 30;
            if (Math.Abs(bar.DeltaPercentage) > 0.75) score += 25;
            if (bar.FormationTime.TotalSeconds < 10) score += 20;

            if (score > 70) return SignalStrength.Critical;
            if (score > 50) return SignalStrength.Strong;
            if (score > 30) return SignalStrength.Moderate;
            return SignalStrength.Weak;
        }

        private string GenerateDescription(SignalType type, TickBar bar)
        {
            return $"{type}: Vol={bar.Volume:F2}, Delta={bar.DeltaPercentage:P}, OI_Imb={bar.OpenPosAskChange - bar.OpenPosBidChange:F2}";
        }

        private Dictionary<string, double> ExtractMetrics(TickBar bar)
        {
            return new Dictionary<string, double>
            {
                ["Volume"] = bar.Volume,
                ["Delta"] = bar.Delta,
                ["DeltaPercentage"] = bar.DeltaPercentage,
                ["OpenPosImbalance"] = bar.OpenPosAskChange - bar.OpenPosBidChange,
                ["PriceImpact"] = bar.PriceImpact,
                ["FormationSeconds"] = (double)bar.FormationTime.TotalSeconds
            };
        }

        private double CalculateConfidence(SignalType type, TickBar bar)
        {
            // Calculate confidence level based on multiple factors
            var confidence = 0.5;

            if (bar.Volume > _analyzer.GetAverageVolume(20) * 2) confidence += 0.2;
            if (Math.Abs(bar.DeltaPercentage) > 0.7) confidence += 0.15;
            if (_analyzer.GetATR() > 0 && bar.PriceRange > _analyzer.GetATR() * 0.8) confidence += 0.15;

            return Math.Min(confidence, 1.0);
        }
    }

    public class ImbalanceConfig
    {
        public double BearishMultiplier { get; set; } = 2.5;
        public double BullishMultiplier { get; set; } = 2.5;
        public double DeltaThreshold { get; set; } = 0.6;
        public double VolumeRatio { get; set; } = 1.8;
        public int FastTickThreshold { get; set; } = 15;
        public double MinOpenPosChange { get; set; } = 50;
        public double VolumeMultiplier { get; set; } = 2;
        public double MinPriceMovement { get; set; } = 0.5;
        public double ExtremeImbalanceThreshold { get; set; } = 100;
        public double ExtremeDeltaThreshold { get; set; } = 0.75;
        public double ExtremeRatioThreshold { get; set; } = 4;
        public int WhaleTickThreshold { get; set; } = 8;
        public double WhaleVolumeThreshold { get; set; } = 100;
        public double WhaleImbalanceThreshold { get; set; } = 75;
        public double WhalePriceImpact { get; set; } = 0.3;
        public int WhaleTimeThreshold { get; set; } = 30;
    }
}
