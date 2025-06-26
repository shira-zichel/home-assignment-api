using FluentValidation;
using HomeAssignment.Configuration;
using HomeAssignment.Decorators;
using HomeAssignment.DTOs;
using HomeAssignment.Extensions;
using HomeAssignment.Factories;
using HomeAssignment.Repositories;
using HomeAssignment.Services;
using HomeAssignment.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

// CRITICAL: Enable PII logging to see actual token values in debug
IdentityModelEventSource.ShowPII = true;

// Add configuration
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.Configure<StorageSettings>(
    builder.Configuration.GetSection("StorageSettings"));
builder.Services.Configure<CacheSettings>(
    builder.Configuration.GetSection("CacheSettings"));
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

// Get settings for early access
var cacheSettings = builder.Configuration.GetSection("CacheSettings").Get<CacheSettings>() ?? new CacheSettings();
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();



// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add JWT Authentication with detailed logging
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // For development
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        RequireExpirationTime = true,
        RequireSignedTokens = true,
        // Critical: Map role claims correctly
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.Name
    };

    // Add detailed event logging for debugging
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var authHeader = context.Request.Headers.Authorization.ToString();
            Console.WriteLine($"?? Received Authorization Header: '{authHeader}'");

            if (authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
            }

            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"docker --version\r\nJWT Authentication FAILED: {context.Exception.Message}");
            Console.WriteLine($"Exception Type: {context.Exception.GetType().Name}");
            Console.WriteLine($"Full Exception: {context.Exception}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var username = context.Principal?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
            var role = context.Principal?.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";
            var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

            Console.WriteLine($"? JWT Token VALIDATED successfully!");
            Console.WriteLine($"   User: {username}");
            Console.WriteLine($"   Role: {role}");
            Console.WriteLine($"   UserID: {userId}");

            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine($"JWT Challenge triggered: {context.Error}");
            Console.WriteLine($"Error Description: {context.ErrorDescription}");
            return Task.CompletedTask;
        },
        OnForbidden = context =>
        {
            Console.WriteLine($"JWT Forbidden: User authenticated but lacks permissions");
            return Task.CompletedTask;
        }
    };
});

// Add Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("Admin", "User"));
});

// Enhanced Swagger with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Home Assignment API", Version = "v1" });
    c.EnableAnnotations();

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add Caching Services
if (cacheSettings.UseRedis)
{
    Console.WriteLine($"Configuring Redis cache...");

    // Configure Redis connection
    builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
    {
        var connectionString = cacheSettings.RedisConnectionString;
        var configuration = ConfigurationOptions.Parse(connectionString);
        configuration.AbortOnConnectFail = false;
        configuration.ConnectRetry = 3;
        configuration.ConnectTimeout = 5000;

        return ConnectionMultiplexer.Connect(configuration);
    });

    // Add Redis distributed cache
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = cacheSettings.RedisConnectionString;
        options.InstanceName = "HomeAssignment";
    });

    // Register Redis cache service
    builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();

    Console.WriteLine("Redis caching enabled");
}
else
{
    builder.Services.AddMemoryCache();
    Console.WriteLine("Using memory caching");
}

// Add AutoMapper
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

// Add FluentValidation
builder.Services.AddScoped<IValidator<CreateDataItemDto>, CreateDataItemValidator>();
builder.Services.AddScoped<IValidator<UpdateDataItemDto>, UpdateDataItemValidator>();
builder.Services.AddScoped<IValidator<int>, IdValidator>();


// Addr Polly Policies
builder.Services.AddPollyPolicies();

// Register Authentication Services
builder.Services.AddScoped<IUserRepository, InMemoryUserRepository>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Register File Cache Service
builder.Services.AddSingleton<IFileCacheService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<FileCacheService>>();
    return new FileCacheService(
        cacheSettings.FileCachePath,
        cacheSettings.FileCacheDurationMinutes,
        logger);
});

// Register Base Repository implementations
builder.Services.AddSingleton<InMemoryDataRepository>();
builder.Services.AddSingleton<MongoDataRepository>();

// Register Repository
builder.Services.AddScoped<IDataRepository>(provider =>
{
    var storageSettings = builder.Configuration.GetSection("StorageSettings").Get<StorageSettings>();
    var storageType = storageSettings?.StorageType ?? "InMemory";

    IDataRepository baseRepository = storageType.ToLower() switch
    {
        "mongodb" => provider.GetRequiredService<MongoDataRepository>(),
        "inmemory" => provider.GetRequiredService<InMemoryDataRepository>(),
        _ => throw new NotSupportedException($"Storage type '{storageType}' is not supported.")
    };

    var fileCache = provider.GetRequiredService<IFileCacheService>();
    var logger = provider.GetRequiredService<ILogger<CachingDataRepository>>();

    return new CachingDataRepository(
        baseRepository,
        fileCache,
        cacheSettings.UseRedis,
        cacheSettings.CacheDurationMinutes,
        logger,
        cacheSettings.UseRedis ? provider.GetService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>() : null,
        !cacheSettings.UseRedis ? provider.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>() : null);
});

// Register Factory Pattern
builder.Services.AddSingleton<IRepositoryFactory>(provider =>
{
    var storageSettings = builder.Configuration.GetSection("StorageSettings").Get<StorageSettings>();
    return new RepositoryFactory(provider, storageSettings?.StorageType ?? "InMemory");
});

// Register Core Data Service
builder.Services.AddScoped<DataService>();

// Register Service with Decorator Pattern
builder.Services.AddScoped<IDataService>(provider =>
{
    var coreService = provider.GetRequiredService<DataService>();
    var logger = provider.GetRequiredService<ILogger<LoggingDataServiceDecorator>>();
    return new LoggingDataServiceDecorator(coreService, logger);
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Home Assignment API v1");
    c.DocumentTitle = "Home Assignment API - Swagger UI";
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigins");

// CRITICAL: Correct middleware order
Console.WriteLine("?? Setting up Authentication & Authorization middleware...");
app.UseAuthentication();  // Must come first
app.UseAuthorization();   // Must come second

app.MapControllers();

// Log startup info
var repositoryFactory = app.Services.GetRequiredService<IRepositoryFactory>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application started using {StorageType} storage", repositoryFactory.GetCurrentStorageType());
logger.LogInformation("Cache Configuration: Redis={UseRedis}, CacheDuration={CacheDuration}min",
    cacheSettings.UseRedis, cacheSettings.CacheDurationMinutes);
logger.LogInformation("JWT Authentication enabled with issuer: {Issuer}", jwtSettings.Issuer);

Console.WriteLine("App is running. Try these URLs:");
Console.WriteLine("Swagger: https://localhost:7059/swagger");
Console.WriteLine("Test Credentials: GET https://localhost:7059/api/auth/test-credentials");

app.Run();