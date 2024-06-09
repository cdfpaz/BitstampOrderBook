// See https://aka.ms/new-console-template for more information

using Bitstamp.FeederServices;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder();

builder.SetBasePath(Directory.GetCurrentDirectory())
       .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
       .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

IConfiguration config = builder.Build();
var orderBookService = new OrderBookService(config);

Console.WriteLine("Server started");
Console.WriteLine("Press Ctrl-C to quit");

orderBookService.Start();

while (true)
{

    Task task = orderBookService.ShowPricesAsync();
    task.Wait();

    Thread.Sleep(5000);
}

