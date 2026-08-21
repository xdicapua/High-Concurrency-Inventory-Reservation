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

// 2. Endpoint de alta concurrencia para reservar stock
app.MapPost("/api/v1/reservations", async (ReserveStockRequest request, ReserveStockUseCase useCase) =>
{
    var response = await useCase.ExecuteAsync(request);

    if (!response.Success)
    {
        return Results.Conflict(response); // 409 Conflict si no hay stock
    }

    return Results.Ok(response); // 200 OK con ID de reserva y TTL
})
.WithName("ReserveStock");

app.Run();