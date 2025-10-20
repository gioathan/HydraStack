using Hydra.Api.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using StackExchange.Redis;
using Hydra.Api.Caching;

var builder = WebApplication.CreateBuilder(args);

// Connection strings
var pgCs = builder.Configuration.GetConnectionString("Postgres")
             ?? "Host=postgres;Port=5432;Database=hydra;Username=app;Password=app";
var redisCs = builder.Configuration.GetConnectionString("Redis") ?? "redis:6379";

// DI: Npgsql pooled data source, EF Core, Redis
builder.Services.AddSingleton(new NpgsqlDataSourceBuilder(pgCs).Build());
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(pgCs));
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisCs));
builder.Services.AddSingleton<ICache, RedisCache>();

// Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Dev-only: create schema & enable Swagger
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
// app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
