namespace TradingSystem.Exchange.Interfaces
{
    using TradingSystem.Core.Models;

    public interface IExchangeClient
    {
        Task<bool> ConnectAsync();
        void Disconnect();
        //Task DisconnectAsync();
        Task SubscribeToTradesAsync(string symbol);
        Task SubscribeToOrderBookAsync(string symbol);
        Task SubscribeToOpenInterestAsync(string symbol);
        Task<OrderBookSnapshot> GetOrderBookAsync(string symbol, int depth = 100);
        Task<double> GetOpenInterestAsync(string symbol);
        Task<List<Trade>> GetRecentTradesAsync(string symbol, int limit = 1000);
        event EventHandler<Trade> OnTradeReceived;
        event EventHandler<OrderBookSnapshot> OnOrderBookUpdate;
        event EventHandler<double> OnOpenInterestUpdate;
        string ExchangeName { get; }
    }
}
