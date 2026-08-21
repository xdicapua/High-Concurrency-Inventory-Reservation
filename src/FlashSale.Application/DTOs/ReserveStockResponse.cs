namespace FlashSale.Application.DTOs;

public record ReserveStockResponse(
    bool Success, 
    Guid? ReservationId, 
    string Message, 
    DateTime? ExpiresAtUtc
);