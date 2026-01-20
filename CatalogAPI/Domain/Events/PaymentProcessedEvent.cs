namespace CatalogAPI.Domain.Events;

public record PaymentProcessedEvent
{
    public Guid UserId { get; init; }
    public Guid GameId { get; init; }
    public decimal Price { get; init; }
    public string Status { get; init; } // Approved or Rejected

    // Default constructor for MassTransit deserialization
    public PaymentProcessedEvent()
    {
        Status = string.Empty;
    }

    // Primary constructor for convenience
    public PaymentProcessedEvent(Guid userId, Guid gameId, decimal price, string status) : this()
    {
        UserId = userId;
        GameId = gameId;
        Price = price;
        Status = status;
    }
}
