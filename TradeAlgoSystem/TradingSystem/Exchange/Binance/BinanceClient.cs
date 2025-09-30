namespace TradingSystem.Exchange.Binance
{
    using WebSocket4Net;
    using System.Text.Json;
    using TradingSystem.Core.Models;
    using TradingSystem.Exchange.Interfaces;
    using TradingSystem.Helpers.Converters;

    public class BinanceClient : IExchangeClient, IDisposable
    {
        private WebSocket _webSocket;
        private readonly HttpClient _httpClient;
        private readonly string _wsBaseUrl = "wss://fstream.binance.com/ws/";
        private readonly string _restUrl = "https://fapi.binance.com";
        private readonly List<string> _streamNames = new();
        private readonly string _apiKey;
        private readonly string _apiSecret;

        public string ExchangeName => "Binance";

        public event EventHandler<Trade> OnTradeReceived;
        public event EventHandler<OrderBookSnapshot> OnOrderBookUpdate;
        public event EventHandler<double> OnOpenInterestUpdate;

        public BinanceClient(string apiKey, string apiSecret)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_restUrl);
            _httpClient.DefaultRequestHeaders.Add("X-MBX-APIKEY", _apiKey);
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                _webSocket = new WebSocket(_wsBaseUrl);

                _webSocket.Opened += WebSocketOpened;
                _webSocket.MessageReceived += WebSocketMessageReceived;
                _webSocket.Error += WebSocketError;
                _webSocket.Closed += WebSocketClosed;

                _webSocket.Open();

                // Wait for the WebSocket to open
                await Task.Delay(1000);
                return _webSocket.State == WebSocketState.Open;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Binance connection error: {ex.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            if (_webSocket?.State == WebSocketState.Open)
            {
                _webSocket.Close();
            }

            _webSocket?.Dispose();
        }

        public async Task SubscribeToTradesAsync(string symbol)
        {
            _streamNames.Add($"{symbol.ToLower()}@aggTrade");
            await ReconnectWithStreams();
        }

        public async Task SubscribeToOrderBookAsync(string symbol)
        {
            _streamNames.Add($"{symbol.ToLower()}@depth@100ms");
            await ReconnectWithStreams();
        }

        public async Task SubscribeToOpenInterestAsync(string symbol)
        {
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        var openInterest = await GetOpenInterestAsync(symbol);
                        OnOpenInterestUpdate?.Invoke(this, openInterest);
                        await Task.Delay(5000);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"OI update error: {ex.Message}");
                    }
                }
            });
        }

        private async Task ReconnectWithStreams()
        {
            if (_streamNames.Count == 0) return;

            if (_webSocket?.State == WebSocketState.Open)
            {
                _webSocket.Close();
            }

            var streamUrl = _wsBaseUrl + string.Join("/", _streamNames);
            _webSocket = new WebSocket(streamUrl);

            _webSocket.Opened += WebSocketOpened;
            _webSocket.MessageReceived += WebSocketMessageReceived;
            _webSocket.Error += WebSocketError;
            _webSocket.Closed += WebSocketClosed;

            _webSocket.Open();
        }

        public async Task<OrderBookSnapshot> GetOrderBookAsync(string symbol, int depth = 100)
        {
            var response = await _httpClient.GetAsync($"/fapi/v1/depth?symbol={symbol}&limit={depth}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return ParseOrderBook(json);
            }

            return null;
        }

        public async Task<double> GetOpenInterestAsync(string symbol)
        {
            var response = await _httpClient.GetAsync($"/fapi/v1/openInterest?symbol={symbol}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("openInterest", out var oi)) // 1883770.928
                {
                    return NumericConverter.GetDouble(oi.GetString());
                }
            }

            return 0;
        }

        public async Task<List<Trade>> GetRecentTradesAsync(string symbol, int limit = 1000)
        {
            var response = await _httpClient.GetAsync($"/fapi/v1/aggTrades?symbol={symbol}&limit={limit}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return ParseRecentTrades(json);
            }

            return new List<Trade>();
        }

        private void WebSocketOpened(object sender, EventArgs e)
        {
            Console.WriteLine("WebSocket connected.");
        }

        private void WebSocketMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            ProcessMessage(e.Message);
        }

        private void WebSocketError(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            Console.WriteLine($"WebSocket error: {e.Exception.Message}");
        }

        private void WebSocketClosed(object sender, EventArgs e)
        {
            Console.WriteLine("WebSocket closed.");
        }

        private void ProcessMessage(string message)
        {
            try
            {
                using var doc = JsonDocument.Parse(message);

                if (doc.RootElement.TryGetProperty("e", out var eventType))
                {
                    var eventStr = eventType.GetString();

                    switch (eventStr)
                    {
                        case "aggTrade":
                            ProcessTradeMessage(doc.RootElement);
                            break;
                        case "depthUpdate":
                            ProcessOrderBookMessage(doc.RootElement);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Process message error: {ex.Message}");
            }
        }

        private void ProcessTradeMessage(JsonElement root)
        {
            var trade = new Trade
            {
                Id = root.GetProperty("a").GetInt64().ToString(),
                Price = NumericConverter.GetDouble(root.GetProperty("p").GetString()),
                Volume = NumericConverter.GetDouble(root.GetProperty("q").GetString()),
                Side = root.GetProperty("m").GetBoolean() ? TradeSide.Sell : TradeSide.Buy,
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(root.GetProperty("T").GetInt64()).UtcDateTime,
                Exchange = ExchangeName
            };

            OnTradeReceived?.Invoke(this, trade);
        }

        private void ProcessOrderBookMessage(JsonElement root)
        {
            var snapshot = new OrderBookSnapshot
            {
                Bids = new List<OrderBookLevel>(),
                Asks = new List<OrderBookLevel>(),
                Timestamp = DateTime.UtcNow
            };

            if (root.TryGetProperty("b", out var bids))
            {
                foreach (var bid in bids.EnumerateArray())
                {
                    snapshot.Bids.Add(new OrderBookLevel
                    {
                        Price = NumericConverter.GetDouble(bid[0].GetString()), 
                        Volume = NumericConverter.GetDouble(bid[1].GetString()) 
                    });
                }
            }

            if (root.TryGetProperty("a", out var asks))
            {
                foreach (var ask in asks.EnumerateArray())
                {
                    snapshot.Asks.Add(new OrderBookLevel
                    {
                        Price = NumericConverter.GetDouble(ask[0].GetString()),
                        Volume = NumericConverter.GetDouble(ask[1].GetString())
                    });
                }
            }

            OnOrderBookUpdate?.Invoke(this, snapshot);
        }

        private OrderBookSnapshot ParseOrderBook(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var snapshot = new OrderBookSnapshot
            {
                Bids = new List<OrderBookLevel>(),
                Asks = new List<OrderBookLevel>(),
                Timestamp = DateTime.UtcNow
            };

            if (doc.RootElement.TryGetProperty("bids", out var bids))
            {
                foreach (var bid in bids.EnumerateArray())
                {
                    snapshot.Bids.Add(new OrderBookLevel
                    {
                        Price = NumericConverter.GetDouble(bid[0].GetString()),
                        Volume = NumericConverter.GetDouble(bid[1].GetString())
                    });
                }
            }

            if (doc.RootElement.TryGetProperty("asks", out var asks))
            {
                foreach (var ask in asks.EnumerateArray())
                {
                    snapshot.Asks.Add(new OrderBookLevel
                    {
                        Price = NumericConverter.GetDouble(ask[0].GetString()),
                        Volume = NumericConverter.GetDouble(ask[1].GetString())
                    });
                }
            }

            return snapshot;
        }

        private List<Trade> ParseRecentTrades(string json)
        {
            var trades = new List<Trade>();
            using var doc = JsonDocument.Parse(json);

            foreach (var trade in doc.RootElement.EnumerateArray())
            {
                trades.Add(new Trade
                {
                    Id = trade.GetProperty("a").GetInt64().ToString(),
                    Price = NumericConverter.GetDouble(trade.GetProperty("p").GetString()),
                    Volume = NumericConverter.GetDouble(trade.GetProperty("q").GetString()),
                    Side = trade.GetProperty("m").GetBoolean() ? TradeSide.Sell : TradeSide.Buy,
                    Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(
                        trade.GetProperty("T").GetInt64()).UtcDateTime,
                    Exchange = ExchangeName
                });
            }

            return trades;
        }

        public void Dispose()
        {
            Disconnect();
            _httpClient?.Dispose();
        }

        //public async Task DisconnectAsync()
        //{
        //    if (_webSocket?.State == System.Net.WebSockets.WebSocketState.Open)
        //    {
        //        await _webSocket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
        //            "Closing connection", CancellationToken.None);
        //    }

        //    _cancellationTokenSource?.Cancel();
        //    _webSocket?.Dispose();
        //}
    }
}




