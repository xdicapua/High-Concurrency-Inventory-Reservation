using FlashSale.Application.DTOs;
using FlashSale.Application.Interfaces;
using FlashSale.Domain.Entities;

namespace FlashSale.Application.UseCases;

public class ConfirmReservationUseCase
{
    private readonly IInventoryCacheRepository _cacheRepository;
    private readonly IReservationRepository _reservationRepository;

    public ConfirmReservationUseCase(
        IInventoryCacheRepository cacheRepository,
        IReservationRepository reservationRepository)
    {
        _cacheRepository = cacheRepository;
        _reservationRepository = reservationRepository;
    }

    public async Task<ConfirmReservationResponse> ExecuteAsync(ConfirmReservationRequest request, CancellationToken ct = default)
    {
        var cacheInfo = await _cacheRepository.GetReservationAsync(request.Sku, request.ReservationId);
        if (cacheInfo is null)
        {
            return new ConfirmReservationResponse(false, "La reserva no existe o ya ha expirado.", null);
        }

        var reservation = Reservation.Create(request.ProductId, cacheInfo.UserId, TimeSpan.FromMinutes(10));

        typeof(Reservation).GetProperty(nameof(Reservation.Id))!
            .SetValue(reservation, request.ReservationId);

        reservation.Confirm();

        await _reservationRepository.AddAsync(reservation, ct);
        await _reservationRepository.SaveChangesAsync(ct);

        await _cacheRepository.DeleteReservationAsync(request.Sku, request.ReservationId);

        return new ConfirmReservationResponse(
            Success: true,
            Message: "Orden y reserva confirmadas con éxito en PostgreSQL.",
            ConfirmedAtUtc: DateTime.UtcNow
        );
    }
}