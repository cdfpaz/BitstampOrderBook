using MongoDB.Driver;
using Bitstamp.Domain.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Text;
using System.Globalization;

namespace Bitstamp.Domain.Data
{
    public class OrderBookRepository
    {
        private readonly IMongoDatabase _mongoDatabase;
        private readonly IMongoCollection<BsonDocument> _collection;

        public OrderBookRepository(string? ConnectionString, string? DatabaseName, string? CollectionName)
        {
            var mongoClient = new MongoClient(ConnectionString);

            _mongoDatabase = mongoClient.GetDatabase(DatabaseName);

            var collectionList = _mongoDatabase.ListCollectionNames().ToList();
            bool collectionExists = collectionList.Contains(CollectionName);

            if (!collectionExists)
            {
                _mongoDatabase.CreateCollection(CollectionName);
            }

            _collection = _mongoDatabase.GetCollection<BsonDocument>(CollectionName);
        }

        public void Save(string msg)
        {
            if (msg != null) {
                var document = BsonDocument.Parse(msg);
                _collection.InsertOne(document);
            }
        }

        public static (decimal minBid, decimal maxAsk) GetMinBidAndMaxAsk(OrderBook orderBook)
        {
            var bids = orderBook.Data.Bids.Select(b => decimal.Parse(b[0])).ToList();
            var asks = orderBook.Data.Asks.Select(a => decimal.Parse(a[0])).ToList();

            decimal minBid = bids.Min();
            decimal maxAsk = asks.Max();

            return (minBid, maxAsk);
        }

        public static (decimal averageBid, decimal averageAsk) GetAverageBidAndAsk(OrderBook orderBook)
        {
            var bids = orderBook.Data.Bids.Select(b => new { Price = decimal.Parse(b[0]), Amount = decimal.Parse(b[1]) }).ToList();
            var asks = orderBook.Data.Asks.Select(a => new { Price = decimal.Parse(a[0]), Amount = decimal.Parse(a[1]) }).ToList();

            decimal totalBidPrice = bids.Sum(b => b.Price * b.Amount);
            decimal totalBidAmount = bids.Sum(b => b.Amount);
            decimal averageBid = totalBidPrice / totalBidAmount;

            decimal totalAskPrice = asks.Sum(a => a.Price * a.Amount);
            decimal totalAskAmount = asks.Sum(a => a.Amount);
            decimal averageAsk = totalAskPrice / totalAskAmount;

            return (averageBid, averageAsk);
        }

        public void Summary(string symbol)
        {
            if (_collection != null)
            {
                var filter = Builders<BsonDocument>.Filter.Eq("channel", $"order_book_{symbol}");
                var sort = Builders<BsonDocument>.Sort.Descending("_id");

                StringBuilder sb = new();

                _collection.Find(filter).Sort(sort).Limit(1).ForEachAsync(d => {
                    try
                    {
                        var orderBook = BsonSerializer.Deserialize<OrderBook>(d);

                        var (minBid, maxAsk) = GetMinBidAndMaxAsk(orderBook);
                        var (averageBid, averageAsk) = GetAverageBidAndAsk(orderBook);

                        sb.Append($"{symbol}: Maior Preço {minBid.ToString("0.00", CultureInfo.InvariantCulture)}, ");
                        sb.Append($"Menor Preço {maxAsk.ToString("0.00", CultureInfo.InvariantCulture)}, ");
                        sb.Append($"Media compra: {averageBid.ToString("0.00", CultureInfo.InvariantCulture)}, ");
                        sb.Append($"Media venda {averageAsk.ToString("0.00", CultureInfo.InvariantCulture)}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                });

                Console.WriteLine(sb);
            }
        }
    }
}
