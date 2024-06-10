using MongoDB.Driver;
using Bitstamp.Domain.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Text;
using System.Globalization;

namespace Bitstamp.Domain.Data
{
    public class MarketDataRepository
    {
        private readonly IMongoDatabase _mongoDatabase;
        private readonly IMongoCollection<BsonDocument> _collection;

        public MarketDataRepository(string? ConnectionString, string? DatabaseName, string? CollectionName)
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

        private static (decimal minBid, decimal maxAsk) GetMinBidAndMaxAsk(OrderBook orderBook)
        {
            var bids = orderBook.Data.Bids.Select(b => decimal.Parse(b[0])).ToList();
            var asks = orderBook.Data.Asks.Select(a => decimal.Parse(a[0])).ToList();

            decimal minBid = bids.Min();
            decimal maxAsk = asks.Max();

            return (minBid, maxAsk);
        }

        private static (decimal averageBid, decimal averageAsk) GetAverageBidAndAsk(OrderBook orderBook)
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

        public static (decimal averageBid, decimal averageAsk) GetAverageBidAndAsk(List<OrderBook> orderBooks)
        {
            var allBids = orderBooks.SelectMany(ob => ob.Data.Bids.Select(b => new { Price = decimal.Parse(b[0]), Amount = decimal.Parse(b[1]) })).ToList();
            var allAsks = orderBooks.SelectMany(ob => ob.Data.Asks.Select(a => new { Price = decimal.Parse(a[0]), Amount = decimal.Parse(a[1]) })).ToList();

            decimal totalBidPrice = allBids.Sum(b => b.Price * b.Amount);
            decimal totalBidAmount = allBids.Sum(b => b.Amount);
            decimal averageBid = totalBidAmount != 0 ? totalBidPrice / totalBidAmount : 0;

            decimal totalAskPrice = allAsks.Sum(a => a.Price * a.Amount);
            decimal totalAskAmount = allAsks.Sum(a => a.Amount);
            decimal averageAsk = totalAskAmount != 0 ? totalAskPrice / totalAskAmount : 0;

            return (averageBid, averageAsk);
        }

        private async Task PrintLast5s(string symbol, StringBuilder sb) 
        {
            long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long pastTimestamp = currentTimestamp - 5;

            var filter = Builders<BsonDocument>.Filter.Eq("channel", $"order_book_{symbol}")
                & Builders<BsonDocument>.Filter.Eq("data.timestamp", pastTimestamp.ToString());

            List<OrderBook> orderBooks = new();
            var result = _collection.Find(filter);
            await result.ForEachAsync(d => {
                try
                {
                    var orderBook = BsonSerializer.Deserialize<OrderBook>(d);
                    orderBooks.Add(orderBook);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });

            var (averageBid, averageAsk) = GetAverageBidAndAsk(orderBooks);

            sb.Append($"Avg Bid 5s: {averageBid.ToString("0.00", CultureInfo.InvariantCulture)}, ");
            sb.Append($"Avg Ask 5s: {averageAsk.ToString("0.00", CultureInfo.InvariantCulture)}");

            Console.WriteLine(sb);
        }

        public async Task SummaryAsync(string symbol)
        {
            if (_collection != null)
            {
                var filter = Builders<BsonDocument>.Filter.Eq("channel", $"order_book_{symbol}");
                var sort = Builders<BsonDocument>.Sort.Descending("_id");

                StringBuilder sb = new();
                var result = _collection.Find(filter).Sort(sort).Limit(1);
                await result.ForEachAsync(d => {
                    try
                    {
                        var orderBook = BsonSerializer.Deserialize<OrderBook>(d);

                        var (minBid, maxAsk) = GetMinBidAndMaxAsk(orderBook);
                        var (averageBid, averageAsk) = GetAverageBidAndAsk(orderBook);

                        sb.Append($" -> {symbol}: High {minBid.ToString("0.00", CultureInfo.InvariantCulture)}, ");
                        sb.Append($"Low {maxAsk.ToString("0.00", CultureInfo.InvariantCulture)}, ");
                        sb.Append($"Avg Bid Qty: {averageBid.ToString("0.00", CultureInfo.InvariantCulture)}, ");
                        sb.Append($"Avg Ask Qty: {averageAsk.ToString("0.00", CultureInfo.InvariantCulture)}");

                        Task task = PrintLast5s(symbol, sb);
                        task.Wait();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                });
            }
        }
    }
}
