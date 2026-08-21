namespace FlashSale.Application.DTOs;

public record ConfirmReservationRequest(string Sku, Guid ReservationId, Guid ProductId);