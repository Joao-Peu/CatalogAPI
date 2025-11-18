namespace CatalogAPI.Domain.Events;

public record PaymentProcessedEvent(Guid UserId, Guid GameId, decimal Price, string Status); // Approved or Rejected
