namespace Bitstamp.Domain.Models;

public class ErrorResponse : IResponse
{
    public string? Reason { get; set; }
    public string? Status { get; set; }
}
