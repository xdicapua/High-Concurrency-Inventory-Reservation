namespace FlashSale.Application.Interfaces;

public record CacheReservationInfo(Guid UserId, string Status);

public interface IInventoryCacheRepository
{
    Task<bool> TryReserveStockAsync(string sku, Guid reservationId, Guid userId, TimeSpan ttl);
    Task ReleaseStockAsync(string sku);
    Task<CacheReservationInfo?> GetReservationAsync(string sku, Guid reservationId);
    Task DeleteReservationAsync(string sku, Guid reservationId);
}