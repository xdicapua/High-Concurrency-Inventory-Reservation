namespace FlashSale.Application.DTOs;

public record ConfirmReservationRequest(Guid ReservationId, Guid ProductId);