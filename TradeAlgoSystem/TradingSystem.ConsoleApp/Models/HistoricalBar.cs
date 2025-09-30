namespace TradingSystem.ConsoleApp.Models
{
    public class HistoricalBar
    {
        public int Id { get; set; }
        public int Tick { get; set; }
        public string Volume { get; set; }
        public string Delta { get; set; }
        public string Time { get; set; }
    }
}
