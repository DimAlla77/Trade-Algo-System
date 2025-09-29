namespace TradingSystem.Strategy.Implementation
{
    using TradingSystem.Core.Models;
    using TradingSystem.Strategy.Interfaces;

    public class MarketAnalyzer : IMarketAnalyzer
    {
        private readonly List<TickBar> _historicalBars = new();
        private readonly object _lock = new();

        public double GetAverageVolume(int periods)
        {
            lock (_lock)
            {
                if (_historicalBars.Count < periods)
                    return _historicalBars.Count > 0 ? _historicalBars.Average(b => b.Volume) : 0;

                return _historicalBars.TakeLast(periods).Average(b => b.Volume);
            }
        }

        public double GetATR()
        {
            lock (_lock)
            {
                if (_historicalBars.Count < 14)
                    return 0;

                return _historicalBars.TakeLast(14).Average(b => b.PriceRange);
            }
        }

        public List<AccumulationZone> DetectAccumulationZones(List<TickBar> bars)
        {
            var zones = new List<AccumulationZone>();

            // Large bars analysis (10000-15000 ticks)
            var largeBars = bars.Where(b => b.TickSize >= 10000).ToList();
            zones.AddRange(DetectLargeBarZones(largeBars));

            // Medium bars analysis (1000-5000 ticks)
            var mediumBars = bars.Where(b => b.TickSize >= 1000 && b.TickSize < 5000).ToList();
            zones.AddRange(DetectMediumBarZones(mediumBars));

            // Merge overlapping zones
            return MergeOverlappingZones(zones);
        }

        private List<AccumulationZone> DetectLargeBarZones(List<TickBar> bars)
        {
            var zones = new List<AccumulationZone>();

            for (int i = 0; i < bars.Count - 5; i++)
            {
                var window = bars.Skip(i).Take(5).ToList();

                // Check for accumulation pattern
                if (window.All(b => b.FormationTime.TotalMinutes > 30) &&
                    window.Sum(b => b.Volume) > 2000 &&
                    window.Max(b => b.PriceRange) < GetATR() * 0.5 &&
                    Math.Abs(window.Sum(b => b.OpenPosBidChange - b.OpenPosAskChange)) > 500)
                {
                    zones.Add(new AccumulationZone
                    {
                        PriceLevel = window.Average(b => (b.HighPrice + b.LowPrice) / 2),
                        VolumeAccumulated = window.Sum(b => b.Volume),
                        Duration = window.Last().FormationStartTime - window.First().FormationStartTime,
                        OpenPosImbalance = window.Sum(b => b.OpenPosBidChange - b.OpenPosAskChange),
                        Type = ZoneType.Accumulation,
                        StartTime = window.First().FormationStartTime,
                        EndTime = window.Last().FormationStartTime.Add(window.Last().FormationTime),
                        StrengthScore = CalculateZoneStrength(window)
                    });
                }
            }

            return zones;
        }

        private List<AccumulationZone> DetectMediumBarZones(List<TickBar> bars)
        {
            var zones = new List<AccumulationZone>();

            for (int i = 0; i < bars.Count - 10; i++)
            {
                var window = bars.Skip(i).Take(10).ToList();

                // Check for support/resistance pattern
                var avgPrice = window.Average(b => (b.HighPrice + b.LowPrice) / 2);
                var priceDeviation = window.Average(b => Math.Abs((b.HighPrice + b.LowPrice) / 2 - avgPrice));

                if (priceDeviation < 2 && window.Sum(b => b.Volume) > 500)
                {
                    var deltaSum = window.Sum(b => b.Delta);
                    var zoneType = deltaSum > 0 ? ZoneType.Support : ZoneType.Resistance;

                    zones.Add(new AccumulationZone
                    {
                        PriceLevel = avgPrice,
                        VolumeAccumulated = window.Sum(b => b.Volume),
                        Duration = window.Last().FormationStartTime - window.First().FormationStartTime,
                        OpenPosImbalance = window.Sum(b => b.OpenPosBidChange - b.OpenPosAskChange),
                        Type = zoneType,
                        StartTime = window.First().FormationStartTime,
                        TouchCount = CountTouches(avgPrice, bars),
                        StrengthScore = CalculateZoneStrength(window)
                    });
                }
            }

            return zones;
        }

        private List<AccumulationZone> MergeOverlappingZones(List<AccumulationZone> zones)
        {
            var merged = new List<AccumulationZone>();
            var sorted = zones.OrderBy(z => z.PriceLevel).ToList();

            foreach (var zone in sorted)
            {
                var existing = merged.FirstOrDefault(z =>
                    Math.Abs(z.PriceLevel - zone.PriceLevel) < 3);

                if (existing != null)
                {
                    // Merge zones
                    existing.VolumeAccumulated += zone.VolumeAccumulated;
                    existing.OpenPosImbalance += zone.OpenPosImbalance;
                    existing.StrengthScore = Math.Max(existing.StrengthScore, zone.StrengthScore);
                    existing.TouchCount = Math.Max(existing.TouchCount, zone.TouchCount);
                }
                else
                {
                    merged.Add(zone);
                }
            }

            return merged;
        }

        private int CountTouches(double priceLevel, List<TickBar> bars)
        {
            return bars.Count(b =>
                (b.LowPrice <= priceLevel && b.HighPrice >= priceLevel) ||
                Math.Abs(b.LowPrice - priceLevel) < 1 ||
                Math.Abs(b.HighPrice - priceLevel) < 1);
        }

        private double CalculateZoneStrength(List<TickBar> window)
        {
            double score = 0;

            // Volume factor
            var avgVolume = GetAverageVolume(20);
            if (avgVolume > 0)
                score += (window.Average(b => b.Volume) / avgVolume) * 30;

            // Time factor
            var avgFormationTime = window.Average(b => b.FormationTime.TotalMinutes);
            if (avgFormationTime > 30) score += 20;

            // Price stability factor
            var priceRange = window.Max(b => b.HighPrice) - window.Min(b => b.LowPrice);
            if (priceRange < 2) score += 25;

            // Open interest factor
            var oiImbalance = Math.Abs(window.Sum(b => b.OpenPosBidChange - b.OpenPosAskChange));
            if (oiImbalance > 500) score += 25;

            return Math.Min(score, 100);
        }

        public bool IsBreakout(TickBar currentBar, AccumulationZone zone)
        {
            if (currentBar == null || zone == null) return false;

            // Check if price breaks through zone
            var priceBreakout = (currentBar.ClosePrice > zone.PriceLevel + 2) ||
                               (currentBar.ClosePrice < zone.PriceLevel - 2);

            // Check volume expansion
            var volumeExpansion = currentBar.Volume > GetAverageVolume(10) * 3;

            // Check formation speed
            var fastFormation = currentBar.FormationTime.TotalMinutes < 3;

            return priceBreakout && volumeExpansion && fastFormation;
        }

        public BreakoutInfo AnalyzeBreakout(List<TickBar> bars, AccumulationZone zone)
        {
            var recentBars = bars.TakeLast(5).ToList();
            var breakoutInfo = new BreakoutInfo();

            if (!recentBars.Any()) return breakoutInfo;

            var triggerBar = recentBars.Last();
            var avgVolume = GetAverageVolume(20);

            // Determine breakout type
            if (triggerBar.ClosePrice > zone.PriceLevel)
            {
                breakoutInfo.Type = BreakoutType.Bullish;
                breakoutInfo.PriceExtension = triggerBar.ClosePrice - zone.PriceLevel;
            }
            else
            {
                breakoutInfo.Type = BreakoutType.Bearish;
                breakoutInfo.PriceExtension = zone.PriceLevel - triggerBar.ClosePrice;
            }

            // Calculate volume expansion
            if (avgVolume > 0)
                breakoutInfo.VolumeExpansion = triggerBar.Volume / avgVolume;

            // Determine grade
            if (zone.VolumeAccumulated > 1000 && breakoutInfo.VolumeExpansion > 5 &&
                Math.Abs(triggerBar.DeltaPercentage) > 0.85)
            {
                breakoutInfo.Grade = BreakoutGrade.Explosive;
                breakoutInfo.Strength = 0.9;
            }
            else if (zone.VolumeAccumulated > 500 && breakoutInfo.VolumeExpansion > 3 &&
                    Math.Abs(triggerBar.DeltaPercentage) > 0.7)
            {
                breakoutInfo.Grade = BreakoutGrade.Strong;
                breakoutInfo.Strength = 0.7;
            }
            else
            {
                breakoutInfo.Grade = BreakoutGrade.Moderate;
                breakoutInfo.Strength = 0.5;
            }

            // Check for confirmation
            breakoutInfo.IsConfirmed = recentBars.TakeLast(3).All(b =>
                (breakoutInfo.Type == BreakoutType.Bullish ?
                    b.ClosePrice > zone.PriceLevel :
                    b.ClosePrice < zone.PriceLevel));

            // Check for false breakout
            if (recentBars.Count >= 3)
            {
                var returnedToZone = breakoutInfo.Type == BreakoutType.Bullish ?
                    recentBars.Any(b => b.ClosePrice < zone.PriceLevel) :
                    recentBars.Any(b => b.ClosePrice > zone.PriceLevel);

                if (returnedToZone)
                {
                    breakoutInfo.Type = BreakoutType.False;
                    breakoutInfo.IsConfirmed = false;
                }
            }

            return breakoutInfo;
        }

        public void UpdateHistoricalBars(TickBar bar)
        {
            lock (_lock)
            {
                _historicalBars.Add(bar);

                // Keep only last 1000 bars
                if (_historicalBars.Count > 1000)
                    _historicalBars.RemoveAt(0);
            }
        }
    }
}
