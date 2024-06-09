using Bitstamp.Domain.Data;
using Microsoft.Extensions.Configuration;

namespace Bitstamp.FeederServices
{
    public class OrderBookService
    {
        private readonly WebSocketService _webSocketService;
        private readonly OrderBookRepository _repository;

        private List<string> _subscribeItems = new();

        public OrderBookService(IConfiguration config)
        {
            var connectionString = config["StoreDatabase:ConnectionString"];
            var databaseName = config["StoreDatabase:DatabaseName"];
            var collectionName = config["StoreDatabase:CollectionName"];

            if (string.IsNullOrEmpty(connectionString) 
                || string.IsNullOrEmpty(databaseName) 
                || string.IsNullOrEmpty(collectionName)) 
            {
                throw new Exception("Missing StoreDatabase configuration");
            }

            var subscribeItems = config.GetSection("Bitstamp:SubscribeItems");
            foreach (var pair in subscribeItems.GetChildren())
            {
                if (pair != null)
                {
                    string? value = pair.Value;
                    if (!string.IsNullOrEmpty(value))
                    {
                        _subscribeItems.Add(value);
                    }
                }
            }

            var url = config["Bitstamp:Url"];

            if (string.IsNullOrEmpty(url))
            {
                throw new Exception("Missing WebSocket url configuration");
            }

            if (_subscribeItems.Count == 0)
            {
                throw new Exception("No symbols to subscribe");
            }

            _repository = new OrderBookRepository(connectionString, databaseName, collectionName);

            _webSocketService = new WebSocketService(url, _subscribeItems);
            _webSocketService.OnOrderBookUpdate += OrderBookUpdate;
        }

        private void OrderBookUpdate(string msg)
        {
            _repository.Save(msg);
        }

        public void Start()
        {
            _webSocketService.Start();
        }

        public async Task ShowPricesAsync()
        {
            Console.WriteLine($"Market summary at {DateTime.Now}");
            foreach (var pair in _subscribeItems)
            {
                await _repository.SummaryAsync(pair);
            }
        }
    }
}
