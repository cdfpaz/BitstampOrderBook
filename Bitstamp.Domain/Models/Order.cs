namespace Bitstamp.Domain.Models;

public class Order
{
    public string? Symbol { get; set; }
    public int Side { get; set; }
    public int Qty { get; set; }
}
