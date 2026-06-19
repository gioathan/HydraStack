using Hydra.Api.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using StackExchange.Redis;
using Asp.Versioning;
using Hydra.Api.Caching;
using Hydra.Api.Middleware;
using Hydra.Api.Repositories.Venues;
using Hydra.Api.Services.Venues;
using Hydra.Api.Repositories.Customers;
using Hydra.Api.Services.Customers;
using Hydra.Api.Repositories.Users;
using Hydra.Api.Services.Users;
using Hydra.Api.Repositories.Bookings;
using Hydra.Api.Services.Bookings;
using Hydra.Api.Repositories.VenuePhotos;
using Hydra.Api.Repositories.VenuePricing;
using Hydra.Api.Repositories.VenueTypes;
using Hydra.Api.Services.VenueTypes;
using Hydra.Api.Repositories.Ratings;
using Hydra.Api.Services.Ratings;
using Hydra.Api.Repositories.VenueEvents;
using Serilog;
using Serilog.Events;
using Hydra.Api.Auth;
using Hydra.Api.Configuration;
using Hydra.Api.Services.GooglePlaces;
using Hydra.Api.Services.Notifications;
using Hydra.Api.Services.Email;
using Hydra.Api.Services.Auth;
using Resend;
using Log = Serilog.Log;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/api-.json",
        formatter: new Serilog.Formatting.Compact.CompactJsonFormatter(),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 100_000_000,
        rollOnFileSizeLimit: true)
    .CreateLogger();

try
{
    Log.Information("Starting Hydra API application");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    var pgCs = builder.Configuration.GetConnectionString("Postgres");
    if (string.IsNullOrWhiteSpace(pgCs))
        throw new InvalidOperationException(
            "ConnectionStrings:Postgres must be configured. Set it via environment variable or user secrets.");

    var redisCs = builder.Configuration.GetConnectionString("Redis");
    if (string.IsNullOrWhiteSpace(redisCs))
        throw new InvalidOperationException(
            "ConnectionStrings:Redis must be configured. Set it via environment variable or user secrets.");

    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseNpgsql(pgCs)
    );

    builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisCs));
    builder.Services.AddSingleton<ICache, RedisCache>();

    builder.Services.AddScoped<IVenueRepository, VenueRepository>();
    builder.Services.AddScoped<IVenuePhotoRepository, VenuePhotoRepository>();
    builder.Services.AddScoped<IVenuePricingRepository, VenuePricingRepository>();
    builder.Services.AddScoped<IVenueService, VenueService>();
    builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
    builder.Services.AddScoped<ICustomerService, CustomerService>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IBookingRepository, BookingRepository>();
    builder.Services.AddScoped<IBookingService, BookingService>();
    builder.Services.AddScoped<IVenueTypeRepository, VenueTypeRepository>();
    builder.Services.AddScoped<IVenueTypeService, VenueTypeService>();
    builder.Services.AddScoped<IRatingRepository, RatingRepository>();
    builder.Services.AddScoped<IRatingService, RatingService>();
    builder.Services.AddScoped<IVenueEventRepository, VenueEventRepository>();
    builder.Services.AddScoped<IVenueEventService, VenueEventService>();

    builder.Services.Configure<GooglePlacesSettings>(builder.Configuration.GetSection("GooglePlaces"));
    builder.Services.AddHttpClient("GooglePlaces");
    builder.Services.AddScoped<IGooglePlacesService, GooglePlacesService>();

    builder.Services.Configure<GoogleAuthSettings>(builder.Configuration.GetSection("GoogleAuth"));

    // Cloudflare R2 storage
    builder.Services.Configure<Hydra.Api.Configuration.CloudflareR2Settings>(
        builder.Configuration.GetSection("CloudflareR2"));

    builder.Services.AddHttpClient("R2");
    builder.Services.AddScoped<Hydra.Api.Services.Storage.IStorageService, Hydra.Api.Services.Storage.CloudflareR2Service>();

    builder.Services.AddHttpClient("Expo", client =>
    {
        client.BaseAddress = new Uri("https://exp.host");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });
    builder.Services.AddScoped<IExpoPushService, ExpoPushService>();
    builder.Services.AddSingleton<INotificationQueue, NotificationQueue>();
    builder.Services.AddHostedService<NotificationWorker>();
    builder.Services.AddHostedService<RatingNotificationWorker>();

    var resendApiKey = builder.Configuration["Resend:ApiKey"];
    if (string.IsNullOrWhiteSpace(resendApiKey))
        throw new InvalidOperationException(
            "Resend:ApiKey is not configured. Set it via environment variable RESEND__APIKEY or user secrets.");

    builder.Services.AddResend(o => o.ApiToken = resendApiKey);
    builder.Services.AddScoped<IEmailService, ResendEmailService>();
    builder.Services.AddScoped<IAuthEmailService, AuthEmailService>();

    builder.Services.AddScoped<DatabaseSeeder>();

    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
    builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

    var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

    if (string.IsNullOrWhiteSpace(jwtSettings?.Secret))
        throw new InvalidOperationException(
            "JwtSettings:Secret is not configured. Set it via environment variable JWTSETTINGS__SECRET or user secrets.");

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "http://localhost:4200"
            )
            .AllowAnyMethod()
            .WithHeaders("Content-Type", "Authorization")
            .AllowCredentials();
        });
    });

    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader());
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    builder.Services.AddRateLimiter(options =>
    {
        // Strict policy for registration and password changes: 5 requests per 15 min per IP
        options.AddSlidingWindowLimiter("auth", opt =>
        {
            opt.PermitLimit = 5;
            opt.Window = TimeSpan.FromMinutes(15);
            opt.SegmentsPerWindow = 3;
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 0;
        });

        // Moderate policy for booking creation: 20 requests per minute per IP
        options.AddSlidingWindowLimiter("bookings", opt =>
        {
            opt.PermitLimit = 20;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.SegmentsPerWindow = 2;
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 0;
        });

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    builder.Services.AddHealthChecks();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // ========================================
    // SWAGGER WITH JWT SUPPORT
    // ========================================
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Hydra Booking API",
            Version = "v1",
            Description = "Restaurant booking management system with JWT authentication"
        });

        // Add JWT Authentication to Swagger
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT token in the text input below.\n\nExample: \"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }

    // --seed flag: run seeder and exit (for staging/CI use)
    if (args.Contains("--seed"))
    {
        // using var seedScope = app.Services.CreateScope();
        // var seeder = seedScope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        // await seeder.SeedAsync();
        // return;
    }

    // Seed development data automatically on startup
    if (app.Environment.IsDevelopment())
    {
        using var seedScope = app.Services.CreateScope();
        var seeder = seedScope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
    }

    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
            diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());
        };
    });

    app.UseGlobalExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseCors();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    Log.Information("Hydra API started successfully");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
