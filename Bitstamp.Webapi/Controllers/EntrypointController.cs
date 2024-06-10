using Bitstamp.Domain.Data;
using Bitstamp.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace webapi.Controllers;

[ApiController]
[Route("[controller]")]
public class EntrypointController : ControllerBase
{

    private readonly OrdersRepository _repository;

    public EntrypointController(IConfiguration config)
    {
        var connectionString = config["Data:ConnectionString"];
        var databaseName = config["Data:OrderData:DatabaseName"];
        var collectionName = config["Data:OrderData:CollectionName"];
        var marketDatadatabaseName = config["Data:MarketData:DatabaseName"];
        var marketDataCollectionName = config["Data:MarketData:CollectionName"];

        if (string.IsNullOrEmpty(connectionString)
            || string.IsNullOrEmpty(databaseName)
            || string.IsNullOrEmpty(collectionName)
            || string.IsNullOrEmpty(marketDatadatabaseName)
            || string.IsNullOrEmpty(marketDataCollectionName))
        {
            throw new Exception("Missing StoreDatabase configuration");
        }

        _repository = new OrdersRepository(connectionString, databaseName, collectionName, marketDatadatabaseName, marketDataCollectionName);
    }

    [HttpPost(Name = "EntryPoint")]
    [Consumes("application/json")]
    public async Task<IActionResult> EntryPointAsync([FromBody] Order request)
    {
        if ( request.Qty == 0)
        {
            var error = new ErrorResponse
            {
                Reason = "Qty cant be 0",
                Status = "error",
            };

            return Ok(error);
        }

        if (string.IsNullOrEmpty(request.Symbol))
        {
            var error = new ErrorResponse
            {
                Reason = "Symbol cant be null",
                Status = "error",
            };

            return Ok(error);
        }

        if (_repository != null)
        {
            var response = await _repository.Match(request);
            return Ok(response);
        }

        var InternalError = new ErrorResponse
        {
            Reason = "Internal error",
            Status = "error",
        };

        return Ok(InternalError);

    }
}
