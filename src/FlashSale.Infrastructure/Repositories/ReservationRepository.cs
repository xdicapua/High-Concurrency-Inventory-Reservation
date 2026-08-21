using FlashSale.Application.Interfaces;
using FlashSale.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlashSale.Infrastructure.Persistence.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly ApplicationDbContext _context;

    public ReservationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        await _context.Reservations.AddAsync(reservation, cancellationToken);
    }

    public async Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Reservations.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}