using MongoDB.Driver;
using Bitstamp.Domain.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Globalization;

namespace Bitstamp.Domain.Data
{
    public class OrdersRepository
    {
        private readonly IMongoCollection<BsonDocument> _marketDataCollection;
        private readonly IMongoCollection<BsonDocument> _orderCollection;

        private int _orderId = 0;

        public OrdersRepository(string? ConnectionString, string? DatabaseName, string? CollectionName, string? MarketDatabaseName, string? MarketCollectionName)
        {
            var mongoClient = new MongoClient(ConnectionString);

            var marketDataDB = mongoClient.GetDatabase(MarketDatabaseName);

            var collectionList = marketDataDB.ListCollectionNames().ToList();
            bool collectionExists = collectionList.Contains(MarketCollectionName);

            if (!collectionExists)
            {
                marketDataDB.CreateCollection(MarketCollectionName);
            }

            _marketDataCollection = marketDataDB.GetCollection<BsonDocument>(MarketCollectionName);

            var ordersDB = mongoClient.GetDatabase(DatabaseName);

            collectionList = ordersDB.ListCollectionNames().ToList();
            collectionExists = collectionList.Contains(CollectionName);

            if (!collectionExists)
            {
                ordersDB.CreateCollection(CollectionName);
            }

            _orderCollection = ordersDB.GetCollection<BsonDocument>(CollectionName);
        }

        public async Task<IResponse> Match(Order request)
        {
            _orderCollection.InsertOne(request.ToBsonDocument());

            if (_marketDataCollection != null)
            {
                decimal totalValue = 0m;
                decimal accumulatedQuantity = 0m;

                var filter = Builders<BsonDocument>.Filter.Eq("channel", $"order_book_{request.Symbol}");
                var sort = Builders<BsonDocument>.Sort.Descending("_id");

                var count = await _marketDataCollection.CountDocumentsAsync(filter);
                if ( count > 0)
                {
                    List<List<string>> Trades = new();
                    var result = _marketDataCollection.Find(filter).Sort(sort).Limit(1);
                    await result.ForEachAsync(d => {
                        try
                        {
                            var orderBook = BsonSerializer.Deserialize<OrderBook>(d);

                            var orderBookData = orderBook.Data;
                            var orderList = request.Side == 0 ? orderBookData.Bids : orderBookData.Asks;

                            foreach (var order in orderList)
                            {
                                decimal price = decimal.Parse(order[0]);
                                decimal orderQuantity = decimal.Parse(order[1]);

                                Trades.Add(order);
                                if (accumulatedQuantity + orderQuantity >= request.Qty)
                                {
                                    decimal remainingQuantity = request.Qty - accumulatedQuantity;
                                    totalValue += remainingQuantity * price;
                                    break;
                                }
                                else
                                {
                                    totalValue += orderQuantity * price;
                                    accumulatedQuantity += orderQuantity;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    });
                    _orderId++;
                    return new BuySellOrderResponse
                    {
                        Id = $"orderId {_orderId}",
                        Market = request.Symbol,
                        Side = request.Side,
                        Date = DateTime.Now,
                        Trades = Trades,
                        Price = totalValue.ToString("0.00", CultureInfo.InvariantCulture),
                        Amount = accumulatedQuantity.ToString("0.00", CultureInfo.InvariantCulture),
                    };
                }
                else
                {
                    return new ErrorResponse
                    {
                        Reason = $"No book for {request.Symbol}",
                        Status = "error"
                    };
                }
            }
            else
            {
                return new ErrorResponse
                {
                    Reason = "Internal error",
                    Status = "error"
                };
            }
        }
    }
}
