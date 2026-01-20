namespace CatalogAPI.Infrastructure.Repositories;

public interface IUserLibraryRepository
{
    Task AddGameToUserAsync(Guid userId, Guid gameId);
    Task<bool> ExistsGameToUserAsync(Guid userId, Guid gameId);
    Task<IEnumerable<Guid>> GetUserLibraryAsync(Guid userId);
}
