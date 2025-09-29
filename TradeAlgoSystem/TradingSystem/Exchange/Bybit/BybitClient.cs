namespace TradingSystem.Exchange.Bybit
{
    using WebSocket4Net;
    using System.Text.Json;
    using System.Globalization;
    using TradingSystem.Core.Models;
    using TradingSystem.Exchange.Interfaces;

    public class BybitClient : IExchangeClient, IDisposable
    {
        private WebSocket _webSocket;
        private readonly HttpClient _httpClient;
        private readonly string _wsUrl = "wss://stream.bybit.com/v5/public/linear";
        private readonly string _restUrl = "https://api.bybit.com";
        private readonly string _apiKey;
        private readonly string _apiSecret;

        public string ExchangeName => "Bybit";

        public event EventHandler<Trade> OnTradeReceived;
        public event EventHandler<OrderBookSnapshot> OnOrderBookUpdate;
        public event EventHandler<double> OnOpenInterestUpdate;

        public BybitClient(string apiKey, string apiSecret)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_restUrl);
            _httpClient.DefaultRequestHeaders.Add("X-BYBIT-APIKEY", _apiKey);
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                _webSocket = new WebSocket(_wsUrl);

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
                Console.WriteLine($"Bybit connection error: {ex.Message}");
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
            var subscribeMessage = new
            {
                op = "subscribe",
                args = new[] { $"publicTrade.{symbol}" }
            };

            await SendMessageAsync(JsonSerializer.Serialize(subscribeMessage));
        }

        public async Task SubscribeToOrderBookAsync(string symbol)
        {
            var subscribeMessage = new
            {
                op = "subscribe",
                args = new[] { $"orderbook.1.{symbol}" }
            };

            await SendMessageAsync(JsonSerializer.Serialize(subscribeMessage));
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

        public async Task<OrderBookSnapshot> GetOrderBookAsync(string symbol, int depth = 100)
        {
            var response = await _httpClient.GetAsync(
                $"/v5/market/orderbook?symbol={symbol}&category=linear&limit={depth}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return ParseOrderBook(json);
            }

            return null;
        }

        public async Task<double> GetOpenInterestAsync(string symbol)
        {
            var response = await _httpClient.GetAsync(
                $"/v5/market/open-interest?category=linear&symbol={symbol}"); // &intervalTime={interval}&limit={dataLimit}"
                //$"/v5/market/openInterest?symbol={symbol}&category=linear");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("result", out var result) &&
                    result.TryGetProperty("list", out var list))
                {
                    var firstItem = list.EnumerateArray().FirstOrDefault();
                    if (firstItem.TryGetProperty("openInterest", out var oi))
                    {
                        return oi.GetDouble();
                    }
                }
            }

            return 0;
        }

        public async Task<List<Trade>> GetRecentTradesAsync(string symbol, int limit = 1000)
        {
            var response = await _httpClient.GetAsync(
                $"/v5/market/recent-trade?symbol={symbol}&category=linear&limit={limit}");

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

                if (doc.RootElement.TryGetProperty("topic", out var topic))
                {
                    var topicStr = topic.GetString();

                    if (topicStr.StartsWith("publicTrade"))
                    {
                        ProcessTradeMessage(doc.RootElement);
                    }
                    else if (topicStr.StartsWith("orderbook"))
                    {
                        ProcessOrderBookMessage(doc.RootElement);
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
            if (root.TryGetProperty("data", out var data))
            {
                foreach (var trade in data.EnumerateArray())
                {
                    var tradeObj = new Trade
                    {
                        Id = trade.GetProperty("i").GetString(),
                        Price = trade.GetProperty("p").GetDouble(),
                        Volume = trade.GetProperty("v").GetDouble(),
                        Side = trade.GetProperty("S").GetString() == "Buy"
                            ? TradeSide.Buy : TradeSide.Sell,
                        Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(
                            trade.GetProperty("T").GetInt64()).UtcDateTime,
                        Exchange = ExchangeName
                    };

                    OnTradeReceived?.Invoke(this, tradeObj);
                }
            }
        }

        private void ProcessOrderBookMessage(JsonElement root)
        {
            if (root.TryGetProperty("data", out var data))
            {
                var snapshot = new OrderBookSnapshot
                {
                    Bids = new List<OrderBookLevel>(),
                    Asks = new List<OrderBookLevel>(),
                    Timestamp = DateTime.UtcNow
                };

                if (data.TryGetProperty("b", out var bids))
                {
                    foreach (var bid in bids.EnumerateArray())
                    {
                        snapshot.Bids.Add(new OrderBookLevel
                        {
                            Price = double.Parse(bid[0].GetString(), CultureInfo.InvariantCulture),
                            Volume = double.Parse(bid[1].GetString(), CultureInfo.InvariantCulture)
                        });
                    }
                }

                if (data.TryGetProperty("a", out var asks))
                {
                    foreach (var ask in asks.EnumerateArray())
                    {
                        snapshot.Asks.Add(new OrderBookLevel
                        {
                            Price = double.Parse(ask[0].GetString(), CultureInfo.InvariantCulture),
                            Volume = double.Parse(ask[1].GetString(), CultureInfo.InvariantCulture)
                        });
                    }
                }

                OnOrderBookUpdate?.Invoke(this, snapshot);
            }
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

            if (doc.RootElement.TryGetProperty("result", out var result))
            {
                if (result.TryGetProperty("b", out var bids))
                {
                    foreach (var bid in bids.EnumerateArray())
                    {
                        snapshot.Bids.Add(new OrderBookLevel
                        {
                            Price = double.Parse(bid[0].GetString(), CultureInfo.InvariantCulture),
                            Volume = double.Parse(bid[1].GetString(), CultureInfo.InvariantCulture)
                        });
                    }
                }

                if (result.TryGetProperty("a", out var asks))
                {
                    foreach (var ask in asks.EnumerateArray())
                    {
                        snapshot.Asks.Add(new OrderBookLevel
                        {
                            Price = double.Parse(ask[0].GetString(), CultureInfo.InvariantCulture),
                            Volume = double.Parse(ask[1].GetString(), CultureInfo.InvariantCulture)
                        });
                    }
                }
            }

            return snapshot;
        }

        private List<Trade> ParseRecentTrades(string json)
        {
            var trades = new List<Trade>();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("result", out var result) &&
                result.TryGetProperty("list", out var list))
            {
                foreach (var trade in list.EnumerateArray())
                {
                    trades.Add(new Trade
                    {
                        Price = trade.GetProperty("price").GetDouble(),
                        Volume = trade.GetProperty("size").GetDouble(),
                        Side = trade.GetProperty("side").GetString() == "Buy"
                            ? TradeSide.Buy : TradeSide.Sell,
                        Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(
                            trade.GetProperty("time").GetInt64()).UtcDateTime,
                        Exchange = ExchangeName
                    });
                }
            }

            return trades;
        }

        private async Task SendMessageAsync(string message)
        {
            if (_webSocket?.State == WebSocketState.Open)
            {
                _webSocket.Send(message);
            }
        }

        public void Dispose()
        {
            Disconnect();
            _httpClient?.Dispose();
        }
    }
}

