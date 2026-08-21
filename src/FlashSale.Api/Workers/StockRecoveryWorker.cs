using FlashSale.Application.Interfaces;
using StackExchange.Redis;

namespace FlashSale.Api.Workers;

public class StockRecoveryWorker : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StockRecoveryWorker> _logger;

    public StockRecoveryWorker(
        IConnectionMultiplexer redis,
        IServiceProvider serviceProvider,
        ILogger<StockRecoveryWorker> logger)
    {
        _redis = redis;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = _redis.GetSubscriber();

        subscriber.Subscribe("__keyevent@0__:expired", async (channel, message) =>
        {
            var expiredKey = message.ToString();

            if (expiredKey.StartsWith("reservation:"))
            {
                var parts = expiredKey.Split(':');
                if (parts.Length == 3)
                {
                    var sku = parts[1];
                    var reservationId = parts[2];

                    _logger.LogWarning("La reserva {ReservationId} del SKU {Sku} expiró. Devolviendo stock...", reservationId, sku);

                    using var scope = _serviceProvider.CreateScope();
                    var cacheRepository = scope.ServiceProvider.GetRequiredService<IInventoryCacheRepository>();

                    await cacheRepository.ReleaseStockAsync(sku);

                    _logger.LogInformation("Stock del SKU {Sku} incrementado exitosamente.", sku);
                }
            }
        });

        _logger.LogInformation("StockRecoveryWorker escuchando expiraciones en Redis...");
        return Task.CompletedTask;
    }
}