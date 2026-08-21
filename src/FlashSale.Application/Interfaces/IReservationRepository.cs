using FlashSale.Domain.Entities;

namespace FlashSale.Application.Interfaces;

public interface IReservationRepository
{
    Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default);
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}