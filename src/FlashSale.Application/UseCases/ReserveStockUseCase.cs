using FlashSale.Application.DTOs;
using FlashSale.Application.Interfaces;

namespace FlashSale.Application.UseCases;

public class ReserveStockUseCase
{
    private readonly IInventoryCacheRepository _cacheRepository;
    private static readonly TimeSpan ReservationDuration = TimeSpan.FromMinutes(10);

    public ReserveStockUseCase(IInventoryCacheRepository cacheRepository)
    {
        _cacheRepository = cacheRepository;
    }

    public async Task<ReserveStockResponse> ExecuteAsync(ReserveStockRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Sku))
        {
            return new ReserveStockResponse(false, null, "El SKU es obligatorio.", null);
        }

        var reservationId = Guid.NewGuid();

        // Ejecutar reserva atómica en caché
        var reserved = await _cacheRepository.TryReserveStockAsync(
            request.Sku,
            reservationId,
            request.UserId,
            ReservationDuration
        );

        if (!reserved)
        {
            return new ReserveStockResponse(false, null, "Stock agotado para este producto.", null);
        }

        var expiresAt = DateTime.UtcNow.Add(ReservationDuration);

        return new ReserveStockResponse(
            Success: true,
            ReservationId: reservationId,
            Message: "Reserva completada exitosamente.",
            ExpiresAtUtc: expiresAt
        );
    }
}