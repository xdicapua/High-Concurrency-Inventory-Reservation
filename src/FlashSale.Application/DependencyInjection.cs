using FlashSale.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace FlashSale.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ReserveStockUseCase>();
        return services;
    }
}