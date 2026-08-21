namespace FlashSale.Domain.Entities;

public class Reservation
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid UserId { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }

    // Constructor privado para forzar el uso del método de fábrica
    private Reservation() { }

    public static Reservation Create(Guid productId, Guid userId, TimeSpan duration)
    {
        var now = DateTime.UtcNow;
        return new Reservation
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            UserId = userId,
            Status = ReservationStatus.Pending,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(duration)
        };
    }

    public void Confirm()
    {
        if (Status != ReservationStatus.Pending)
            throw new InvalidOperationException("Solo se pueden confirmar reservas pendientes.");

        if (DateTime.UtcNow > ExpiresAtUtc)
            throw new InvalidOperationException("La reserva ya ha expirado.");

        Status = ReservationStatus.Confirmed;
    }

    public void Expire()
    {
        if (Status == ReservationStatus.Pending)
        {
            Status = ReservationStatus.Expired;
        }
    }
}