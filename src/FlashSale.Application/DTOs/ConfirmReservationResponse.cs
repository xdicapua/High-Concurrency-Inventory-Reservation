namespace FlashSale.Application.DTOs;

public record ConfirmReservationResponse(
    bool Success, 
    string Message, 
    DateTime? ConfirmedAtUtc
);