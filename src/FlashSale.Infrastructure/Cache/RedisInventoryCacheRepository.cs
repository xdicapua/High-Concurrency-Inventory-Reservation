using FlashSale.Application.Interfaces;
using StackExchange.Redis;

namespace FlashSale.Infrastructure.Cache;

public class RedisInventoryCacheRepository : IInventoryCacheRepository
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    private const string ReserveLuaScript = @"
        local stock = tonumber(redis.call('GET', KEYS[1]) or '0')
        if stock <= 0 then
            return -1
        end
        redis.call('DECR', KEYS[1])
        redis.call('HSET', KEYS[2], 'user_id', ARGV[1], 'status', 'PENDING')
        redis.call('EXPIRE', KEYS[2], tonumber(ARGV[2]))
        return 1
    ";

    private readonly LuaScript _preparedScript;

    public RedisInventoryCacheRepository(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _db = redis.GetDatabase();
        _preparedScript = LuaScript.Prepare(ReserveLuaScript);
    }

    public async Task<bool> TryReserveStockAsync(string sku, Guid reservationId, Guid userId, TimeSpan ttl)
    {
        var keys = new RedisKey[]
        {
            $"item:{sku}:stock",
            $"reservation:{reservationId}"
        };

        var values = new RedisValue[]
        {
            userId.ToString(),
            (int)ttl.TotalSeconds
        };

        var result = await _db.ScriptEvaluateAsync(
            ReserveLuaScript, 
            keys, 
            values
        );

        return (int)result == 1;
    }

    public async Task ReleaseStockAsync(string sku)
    {
        var stockKey = $"item:{sku}:stock";
        await _db.StringIncrementAsync(stockKey);
    }
}