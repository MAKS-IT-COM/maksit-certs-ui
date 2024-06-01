namespace LetsEncryptServer.BackgroundServices {
  public class AutoRenewal : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
      while (!stoppingToken.IsCancellationRequested) {
        // Your background task logic here
        Console.WriteLine("Background service is running.");

        // Simulate some work by delaying for 5 seconds
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
      }
    }

    public override Task StopAsync(CancellationToken stoppingToken) {
      Console.WriteLine("Background service is stopping.");
      return base.StopAsync(stoppingToken);
    }
  }
}
