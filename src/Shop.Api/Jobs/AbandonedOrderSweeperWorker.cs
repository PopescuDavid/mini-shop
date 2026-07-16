using Microsoft.Extensions.Options;

namespace Shop.Api.Jobs;

public class AbandonedOrderSweeperWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<SweeperOptions> options,
    ILogger<AbandonedOrderSweeperWorker> logger) : BackgroundService
{
    private readonly SweeperOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Abandoned-order sweeper started (interval {Interval}s, expiry threshold {Threshold}min).",
            _options.IntervalSeconds, _options.DraftExpiryMinutes);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.IntervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var sweeper = scope.ServiceProvider.GetRequiredService<IAbandonedOrderSweeper>();
                await sweeper.SweepAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Abandoned-order sweep failed.");
            }
        }
    }
}
