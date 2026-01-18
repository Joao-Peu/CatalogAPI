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

// Helper to require configuration values (security best practices)
string Require(string key)
{
    var value = builder.Configuration[key];
    if (string.IsNullOrWhiteSpace(value))
        throw new InvalidOperationException($"Missing required configuration key: {key}. Set it via appsettings or environment variables.");
    return value;
}

// Configuration (environment variables / appsettings)
var rabbitUser =  Environment.GetEnvironmentVariable("RABBITMQ__USERNAME") ??Require("RabbitMQ:UserName");
var rabbitVHost =  Environment.GetEnvironmentVariable("RABBITMQ__VIRTUALHOST") ?? builder.Configuration["RabbitMQ:VirtualHost"] ?? "/";
var rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ__HOST") ?? Require("RabbitMQ:HostName");
var rabbitPass = Environment.GetEnvironmentVariable("RABBITMQ__PASSWORD") ?? Require("RabbitMQ:Password");

// JWT key must be provided via configuration; never default to a hardcoded key
var jwtKey = Require("Jwt:Key");

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

// Repositories (EF Core)
builder.Services.AddScoped<IGameRepository, EfGameRepository>();
builder.Services.AddScoped<IUserLibraryRepository, EfUserLibraryRepository>();

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

        cfg.ConfigureEndpoints(context);
    });
});

// Simple JWT auth placeholder (note: for demo only)
var key = Encoding.ASCII.GetBytes(jwtKey);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true; // enforce HTTPS for tokens
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(2),
        //ValidIssuer = builder.Configuration["JWT:Issuer"],
        //ValidAudience = builder.Configuration["JWT:Audience"]
    };
});

var app = builder.Build();

// Ensure database created and seed demo data (dev only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
