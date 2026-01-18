namespace Shared.Events;

public record OrderPlacedEvent(Guid UserId, Guid GameId, decimal Price);
