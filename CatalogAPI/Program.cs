using CatalogAPI.Application.Services;
using CatalogAPI.Infrastructure.Consumers;
using CatalogAPI.Infrastructure.Persistence;
using CatalogAPI.Infrastructure.Repositories;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configuration (environment variables / appsettings)
var rabbitHost = builder.Configuration["RABBITMQ:Host"] ?? "rabbitmq";
var rabbitVHost = builder.Configuration["RABBITMQ:VirtualHost"] ?? "/";
var rabbitUser = builder.Configuration["RABBITMQ:Username"] ?? "fiap";
var rabbitPass = builder.Configuration["RABBITMQ:Password"] ?? "fiap123";

// JWT key from configuration
var jwtKey = builder.Configuration["JWT:Key"] ?? "very_secret_demo_key_please_change";

// Connection string must be provided via configuration; avoid hardcoded defaults
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__CatalogDb") 
    ?? builder.Configuration.GetConnectionString("CatalogDb")
    ?? builder.Configuration["ConnectionStrings:CatalogDb"]
    ?? throw new InvalidOperationException("Missing ConnectionStrings:CatalogDb. Set it via appsettings or environment variables.");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger + JWT bearer auth
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CatalogAPI", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter JWT Bearer token",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme
        }
    };

    c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            securityScheme,
            Array.Empty<string>()
        }
    });
});

// EF Core DbContext
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseSqlServer(connectionString));

// Health Checks
builder.Services.AddHealthChecks()
    .AddRabbitMQ(
        rabbitConnectionString: $"amqp://{rabbitUser}:{rabbitPass}@{rabbitHost}{rabbitVHost}",
        name: "rabbitmq",
        timeout: TimeSpan.FromSeconds(3),
        tags: new[] { "ready" });

// Database repositories (EF Core)
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IUserLibraryRepository, UserLibraryRepository>();
builder.Services.AddScoped<IOrderGameRepository, OrderGameRepository>();

// Application services
builder.Services.AddScoped<GameService>();

// MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PaymentProcessedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, rabbitVHost, h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });

        cfg.ReceiveEndpoint("catalog-payment-processed", e =>
        {
            e.ConfigureConsumer<PaymentProcessedConsumer>(context);
        });
    });
});

// JWT Authentication
var key = Encoding.ASCII.GetBytes(jwtKey);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(2),
    };
});

var app = builder.Build();

// Ensure database created and seed demo data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    
    try
    {
        db.Database.Migrate();
        
        if (!db.Games.Any())
        {
            db.Games.AddRange(
                new CatalogAPI.Domain.Entities.Game { Id = Guid.NewGuid(), Title = "Cyber Adventure", Description = "Futuristic RPG", Price = 49.99M },
                new CatalogAPI.Domain.Entities.Game { Id = Guid.NewGuid(), Title = "Space Battles", Description = "Multiplayer space shooter", Price = 29.99M }
            );
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
        throw;
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Health Check Endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // Apenas verifica se o app está rodando
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
