namespace Bitstamp.Domain.Models;

public class BuySellOrderResponse : IResponse
{
    public string? Id { get; set; }
    public string? Market { get; set; }
    public int Side { get; set; }
    public DateTime Date { get; set; }
    public string? Price { get; set; }
    public string? Amount { get; set; }
    public List<List<string>> Trades { get; set; }

}
