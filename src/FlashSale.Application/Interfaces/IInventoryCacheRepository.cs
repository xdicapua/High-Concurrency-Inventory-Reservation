namespace FlashSale.Application.Interfaces;

public record CacheReservationInfo(Guid UserId, string Status);

public interface IInventoryCacheRepository
{
    /// <summary>
    /// Intenta reservar de forma atómica 1 unidad de stock en memoria.
    /// </summary>
    /// <param name="sku">Identificador del producto</param>
    /// <param name="reservationId">ID único de la reserva a generar</param>
    /// <param name="userId">ID del usuario solicitante</param>
    /// <param name="ttl">Tiempo de vida de la reserva</param>
    /// <returns>True si se reservó con éxito, False si no hay stock</returns>
    Task<bool> TryReserveStockAsync(string sku, Guid reservationId, Guid userId, TimeSpan ttl);

    /// <summary>
    /// Devuelve 1 unidad de stock al inventario (ej. cancelación o expiración).
    /// </summary>
    Task ReleaseStockAsync(string sku);

    /// <summary>
    /// Obtiene la información de la reserva desde la caché si existe.
    /// </summary>
    Task<CacheReservationInfo?> GetReservationAsync(Guid reservationId);

    /// <summary>
    /// Elimina la reserva de la caché.
    /// </summary>
    Task DeleteReservationAsync(Guid reservationId);
}