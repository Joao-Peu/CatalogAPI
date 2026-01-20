using CatalogAPI.Application.Services;
using CatalogAPI.Infrastructure.Consumers;
using CatalogAPI.Infrastructure.Repositories;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configuration (environment variables / appsettings)
var rabbitHost = builder.Configuration["RABBITMQ:Host"] ?? "rabbitmq";
var rabbitVHost = builder.Configuration["RABBITMQ:VirtualHost"] ?? "/";
var rabbitUser = builder.Configuration["RABBITMQ:Username"] ?? "fiap";
var rabbitPass = builder.Configuration["RABBITMQ:Password"] ?? "fiap123";
var jwtKey = builder.Configuration["JWT:Key"] ?? "very_secret_demo_key_please_change";

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Health Checks
builder.Services.AddHealthChecks()
    .AddRabbitMQ(
        rabbitConnectionString: $"amqp://{rabbitUser}:{rabbitPass}@{rabbitHost}{rabbitVHost}",
        name: "rabbitmq",
        timeout: TimeSpan.FromSeconds(3),
        tags: new[] { "ready" });

// In-memory repositories
builder.Services.AddSingleton<IGameRepository, InMemoryGameRepository>();
builder.Services.AddSingleton<IUserLibraryRepository, InMemoryUserLibraryRepository>();

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

        cfg.ReceiveEndpoint("payment_processed_queue", e =>
        {
            e.ConfigureConsumer<PaymentProcessedConsumer>(context);
        });
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
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

var app = builder.Build();

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
