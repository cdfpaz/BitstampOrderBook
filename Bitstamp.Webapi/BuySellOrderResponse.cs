namespace webapi;

public class BuySellOrderResponse
{
    public string? Id { get; set; }
    public string? Market { get; set; }
    public DateTime Date { get; set; }
    public int Price { get; set; }
    public int Amount { get; set; }
    public string? ClOrderID { get; set; }
}
