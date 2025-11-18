using CatalogAPI.Application.Services;
using CatalogAPI.Domain.Events;
using MassTransit;

namespace CatalogAPI.Infrastructure.Consumers;

public class PaymentProcessedConsumer : IConsumer<PaymentProcessedEvent>
{
    private readonly GameService _gameService;

    public PaymentProcessedConsumer(GameService gameService)
    {
        _gameService = gameService;
    }

    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var evt = context.Message;
        await _gameService.HandlePaymentProcessedAsync(evt);
    }
}
