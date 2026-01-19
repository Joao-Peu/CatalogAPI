namespace CatalogAPI.Domain.Entities;

public class OrderGame
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid GameId { get; set; }
    public bool IsProcessed { get; set; }
}
