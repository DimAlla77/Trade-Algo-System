namespace TradingSystem.Core.Models
{
    /// <summary>
    /// Модель Open Interest (OI) для ф'ючерсних контрактів.
    /// Відображає кількість відкритих позицій у ринку.
    /// </summary>
    public class OpenInterest
    {
        /// <summary>
        /// Символ (наприклад BTCUSDT).
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Біржа-джерело (Binance, Bybit тощо).
        /// </summary>
        public string Exchange { get; set; }

        /// <summary>
        /// Значення Open Interest (у контрактах або монетах).
        /// </summary>
        public decimal Value { get; set; }

        /// <summary>
        /// Час вимірювання (UTC).
        /// </summary>
        public DateTime Timestamp { get; set; }

        public override string ToString()
        {
            return $"{Exchange}:{Symbol} OI={Value} @ {Timestamp:HH:mm:ss}";
        }
    }
}
