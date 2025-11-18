namespace CatalogAPI.Domain.Events;

public record OrderPlacedEvent(Guid UserId, Guid GameId, decimal Price);
