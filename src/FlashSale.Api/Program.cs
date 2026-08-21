using FlashSale.Application;
using FlashSale.Application.DTOs;
using FlashSale.Application.UseCases;
using FlashSale.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 1. Inyección de dependencias de las capas
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// 1. Endpoint para reservar en Redis (RAM)
app.MapPost("/api/v1/reservations", async (ReserveStockRequest request, ReserveStockUseCase useCase) =>
{
    var response = await useCase.ExecuteAsync(request);
    return response.Success ? Results.Ok(response) : Results.Conflict(response);
})
.WithName("ReserveStock");

// 2. Endpoint para confirmar y persistir en PostgreSQL (Disco)
app.MapPost("/api/v1/reservations/confirm", async (ConfirmReservationRequest request, ConfirmReservationUseCase useCase) =>
{
    var response = await useCase.ExecuteAsync(request);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
})
.WithName("ConfirmReservation");

app.Run();