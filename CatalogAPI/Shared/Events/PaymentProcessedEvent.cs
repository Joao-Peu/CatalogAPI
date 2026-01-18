namespace CatalogAPI.Shared.Events;

public record PaymentProcessedEvent(Guid UserId, Guid GameId, decimal Price, string Status);
