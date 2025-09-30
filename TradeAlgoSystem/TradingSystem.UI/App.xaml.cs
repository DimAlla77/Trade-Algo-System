namespace TradingSystem.UI
{
    using System.Windows;

    public partial class App : Application
    {
        public static string Binance_API_Key { get; } = "oizmTNqpsXFJtMrn680tibcnuqwDHAwisCY4GgrCey3KBfdSgEZ5KH0fjmCPF24w";
        public static string Binance_API_Secret { get; } = "IVMa8d61AOP3ogJV5cDAIt7PVCD6nvd2HAwvboJRX83ERGdWagYkNr9ruQ5GvVPU";

        public static string Bybit_API_Key { get; } = "bwhXYZfIBP4OeJrRhT";
        public static string Bybit_API_Secret { get; } = "q5DtFPwRI8jCPMVxhwXzQFBO4lMVnijZlbvu";

        public static string ConnectStr { get; } = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=C:\\Users\\allad\\Desktop\\TradeAlgoSystem\\Trade-Algo-System\\TradeAlgoSystem\\TradingSystem.ConsoleApp\\Database\\TradingSys_Db.mdf;Integrated Security=True";
    }

}
