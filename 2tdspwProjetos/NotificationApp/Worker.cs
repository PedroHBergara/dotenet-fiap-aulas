using Microsoft.AspNetCore.SignalR;

namespace NotificationApp;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHubContext<StockHub> _stockHub;
    private const string stockName = "Basic Stock Name";
    private decimal stockPrice = 100;

    public Worker(ILogger<Worker> logger, IHubContext<StockHub> stockHub)
    {
        _logger = logger;
        _stockHub = stockHub;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                // Simulate stock price change
                Random rnd = new Random();
                decimal stockRaise = rnd.Next(1, 10000);

                string[] stockNames = { "Apple", "Microsoft", "Google", "Amazon", "Netflix", "Meta" };
                var stockName = stockNames[rnd.Next(0, stockNames.Length)];
                await _stockHub.Clients.All.SendAsync("ReceiveStockPrice", stockName, stockRaise);
                _logger.LogInformation("Sent stock price: {stockName} - {stockRaise}", stockName, stockRaise);

                // Wait for 5 seconds before sending the next stock price
                await Task.Delay(5000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending stock price");
            }
        }
    }
}