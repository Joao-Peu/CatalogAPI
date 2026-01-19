namespace CatalogAPI.Domain.Entities;

public class UserLibraryEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid GameId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
