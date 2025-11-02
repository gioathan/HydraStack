using Hydra.Api.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using StackExchange.Redis;
using Hydra.Api.Caching;
using Hydra.Api.Repositories.Venues;
using Hydra.Api.Services.Venues;
using Hydra.Api.Repositories.Customers;
using Hydra.Api.Services.Customers;

var builder = WebApplication.CreateBuilder(args);

var pgCs = builder.Configuration.GetConnectionString("Postgres")
             ?? "Host=postgres;Port=5432;Database=hydra;Username=app;Password=app";
var redisCs = builder.Configuration.GetConnectionString("Redis") ?? "redis:6379";

// Either keep the data source OR just UseNpgsql with conn string (no need for both).
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(pgCs)
);

builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisCs));
builder.Services.AddSingleton<ICache, RedisCache>();

builder.Services.AddScoped<IVenueRepository, VenueRepository>();
builder.Services.AddScoped<IVenueService, VenueService>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Apply pending migrations at startup (dev or prod—up to you)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();   // << replaces EnsureCreated()
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
// app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
