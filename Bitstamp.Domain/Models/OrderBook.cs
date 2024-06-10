using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Bitstamp.Domain.Models;
public class OrderBook
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("data")]
    public OrderBookData Data { get; set; }

    [BsonElement("channel")]
    public string Channel { get; set; }

    [BsonElement("event")]
    public string Event { get; set; }

    public OrderBook() 
    {
        Id = new ObjectId();
        Data = new OrderBookData();
        Channel = string.Empty;
        Event = string.Empty;
    }
}

public class OrderBookData
{
    [BsonElement("timestamp")]
    public string Timestamp { get; set; }

    [BsonElement("microtimestamp")]
    public string Microtimestamp { get; set; }

    [BsonElement("bids")]
    public List<List<string>> Bids { get; set; }

    [BsonElement("asks")]
    public List<List<string>> Asks { get; set; }

    public OrderBookData()
    {
        Timestamp = string.Empty;
        Microtimestamp = string.Empty;
        Bids = new List<List<string>>();
        Asks = new List<List<string>>();
    }
}
