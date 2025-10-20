using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Hydra.Api.Caching;
using Moq;
using StackExchange.Redis;

namespace Hydra.Api.Tests.Unit;

public class RedisCacheTests
{
    private static (RedisCache cache, Mock<IDatabase> dbMock) MakeCache()
    {
        var mux = new Mock<IConnectionMultiplexer>();
        var db = new Mock<IDatabase>();

        mux.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
           .Returns(db.Object);

        var cache = new RedisCache(mux.Object);
        return (cache, db);
    }

    [Fact]
    public async Task GetOrSet_Hit_Returns_Cached_Value_And_Does_Not_Call_Factory()
    {
        var (cache, db) = MakeCache();

        var expected = new { x = 42, s = "hi" };
        var json = JsonSerializer.Serialize(expected, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        // Explicitly provide the optional arg for flags
        db.Setup(d => d.StringGetAsync("k1", CommandFlags.None))
          .ReturnsAsync(json);

        var factoryCalls = 0;

        var value = await cache.GetOrSetAsync(
            "k1",
            TimeSpan.FromMinutes(5),
            factory: _ => { factoryCalls++; return Task.FromResult(expected); },
            ct: CancellationToken.None);

        value.Should().NotBeNull();
        value!.x.Should().Be(42);
        factoryCalls.Should().Be(0);

        // Verify NO write happened — use the 5-arg overload explicitly (avoid optional-arg expression issue)
        db.Verify(d => d.StringSetAsync(
                    It.IsAny<RedisKey>(),
                    It.IsAny<RedisValue>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<When>(),
                    It.IsAny<CommandFlags>()),
                  Times.Never);
    }

    [Fact]
    public async Task GetOrSet_Miss_Calls_Factory_And_Sets_With_TTL()
    {
        var (cache, db) = MakeCache();

        // Cache MISS
        db.Setup(d => d.StringGetAsync("k2", CommandFlags.None))
          .ReturnsAsync(RedisValue.Null);

        var produced = new { n = 7 };
        var ttl = TimeSpan.FromMinutes(10);

        // Set with TTL: match the 5-arg overload and assert TTL within [9,11] minutes (jitter allowance)
        db.Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.Is<TimeSpan?>(t =>
                    t.HasValue &&
                    t.Value.TotalMinutes >= 9.0 &&
                    t.Value.TotalMinutes <= 11.0),
                It.Is<When>(w => w == When.Always),
                It.Is<CommandFlags>(f => f == CommandFlags.None)))
          .ReturnsAsync(true);

        var value = await cache.GetOrSetAsync(
            "k2",
            ttl,
            factory: _ => Task.FromResult(produced),
            jitter: TimeSpan.FromSeconds(60),
            ct: CancellationToken.None);

        value.n.Should().Be(7);

        db.Verify(d => d.StringSetAsync(
                    It.IsAny<RedisKey>(),
                    It.IsAny<RedisValue>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<When>(),
                    It.IsAny<CommandFlags>()),
                  Times.Once);
    }

    [Fact]
    public async Task Token_Get_And_Bump_Work()
    {
        var (cache, db) = MakeCache();

        db.Setup(d => d.StringGetAsync("tok", CommandFlags.None))
          .ReturnsAsync("5");

        var v = await cache.GetTokenAsync("tok");
        v.Should().Be(5);

        db.Setup(d => d.StringIncrementAsync("tok", 1, CommandFlags.None))
          .ReturnsAsync(6);

        var bumped = await cache.BumpTokenAsync("tok");
        bumped.Should().Be(6);
    }

    [Fact]
    public async Task RemoveAsync_Delegates_To_KeyDeleteAsync()
    {
        var (cache, db) = MakeCache();

        db.Setup(d => d.KeyDeleteAsync("delkey", CommandFlags.None))
          .ReturnsAsync(true);

        var ok = await cache.RemoveAsync("delkey");
        ok.Should().BeTrue();

        db.Verify(d => d.KeyDeleteAsync("delkey", CommandFlags.None), Times.Once);
    }
}
