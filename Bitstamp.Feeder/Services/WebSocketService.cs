using Websocket.Client;
using Newtonsoft.Json;

namespace Bitstamp.FeederServices
{
    public class WebSocketService
    {
        const string subscribe_event = "bts:subscribe";
        public class SubscribeMessage
        {
            [JsonProperty("event")]
            public string? Event { get; set; }

            [JsonProperty("data")]
            public SubscribeData? Data { get; set; }
        }

        public class SubscribeData
        {
            [JsonProperty("channel")]
            public string? Channel { get; set; }
        }


        private string _url;
        private WebsocketClient? _client;
        public event Action<string>? OnOrderBookUpdate;
        private List<string> _subscribeItems = new();

        public WebSocketService(string url, List<string> subscribeItems)
        {
            _url = url;
            _subscribeItems = subscribeItems;
        }

        public void Start()
        {
            _client = new WebsocketClient(new Uri(_url));
            _client.ReconnectionHappened.Subscribe(type => {
                Console.WriteLine($"Reconnection {type}");
                Subscribe();
            });

            _client.DisconnectionHappened.Subscribe(type => {
                Console.WriteLine($"Disconnection {type}");
            });

            _client.MessageReceived.Subscribe(msg => HandleMessage(message: msg.Text));
            _client.Start();
        }

        private void Subscribe()
        {
            foreach( var pair in _subscribeItems)
            {
                Console.WriteLine($"Subscribing book for {pair}");
                SubscribeToPair(pair);
            }
        }

        private void SubscribeToPair(string pair)
        {
            if (_client == null)
            {
                Console.WriteLine("Websocket client is null");
                return;
            }

            var subscribeMessage = new SubscribeMessage
            {
                Event = subscribe_event,
                Data = new SubscribeData { Channel = $"order_book_{pair}" }
            };

            _client.Send(JsonConvert.SerializeObject(subscribeMessage));
        }

        private void HandleMessage(string? message)
        {
            if (string.IsNullOrEmpty(message)) return;
            OnOrderBookUpdate?.Invoke(message);
        }
    }
}
