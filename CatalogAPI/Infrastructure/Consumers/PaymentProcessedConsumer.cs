using CatalogAPI.Application.Services;
using MassTransit;
using Shared.Events;

namespace CatalogAPI.Infrastructure.Consumers;

public class PaymentProcessedConsumer(GameService gameService) : IConsumer<PaymentProcessedEvent>
{
    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var evt = context.Message;
        await gameService.HandlePaymentProcessedAsync(evt);
    }
}
