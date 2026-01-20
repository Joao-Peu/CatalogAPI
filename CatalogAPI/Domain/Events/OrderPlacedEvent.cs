namespace CatalogAPI.Domain.Events;

public record OrderPlacedEvent
{
    public Guid UserId { get; init; }
    public Guid GameId { get; init; }
    public decimal Price { get; init; }

    // Default constructor for MassTransit deserialization
    public OrderPlacedEvent()
    {
    }

    // Primary constructor for convenience
    public OrderPlacedEvent(Guid userId, Guid gameId, decimal price) : this()
    {
        UserId = userId;
        GameId = gameId;
        Price = price;
    }
}
