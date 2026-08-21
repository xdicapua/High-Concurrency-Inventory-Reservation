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
        // 1. Validar si la reserva sigue viva en Redis (no expirada)
        var cacheInfo = await _cacheRepository.GetReservationAsync(request.ReservationId);
        if (cacheInfo is null)
        {
            return new ConfirmReservationResponse(false, "La reserva no existe o ya ha expirado.", null);
        }

        // 2. Crear y confirmar la entidad de dominio
        var reservation = Reservation.Create(request.ProductId, cacheInfo.UserId, TimeSpan.FromMinutes(10));
        
        // Asignar el mismo ID generado previamente
        typeof(Reservation).GetProperty(nameof(Reservation.Id))!
            .SetValue(reservation, request.ReservationId);

        reservation.Confirm();

        // 3. Persistir en PostgreSQL
        await _reservationRepository.AddAsync(reservation, ct);
        await _reservationRepository.SaveChangesAsync(ct);

        // 4. Limpiar clave de Redis ya confirmada
        await _cacheRepository.DeleteReservationAsync(request.ReservationId);

        return new ConfirmReservationResponse(
            Success: true, 
            Message: "Orden y reserva confirmadas con éxito en PostgreSQL.", 
            ConfirmedAtUtc: DateTime.UtcNow
        );
    }
}