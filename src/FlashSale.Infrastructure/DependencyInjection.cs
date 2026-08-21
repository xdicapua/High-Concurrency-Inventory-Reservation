using FlashSale.Application.Interfaces;
using FlashSale.Infrastructure.Cache;
using FlashSale.Infrastructure.Persistence;
using FlashSale.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace FlashSale.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Redis
        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(sp => 
            ConnectionMultiplexer.Connect(redisConnectionString));
        services.AddScoped<IInventoryCacheRepository, RedisInventoryCacheRepository>();

        // 2. PostgreSQL + EF Core
        var pgConnectionString = configuration.GetConnectionString("Postgres") 
            ?? "Host=localhost;Port=5432;Database=flash_sale_db;Username=root;Password=secretpassword";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(pgConnectionString));

        services.AddScoped<IReservationRepository, ReservationRepository>();

        return services;
    }
}