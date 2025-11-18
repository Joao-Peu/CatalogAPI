namespace CatalogAPI.Infrastructure.Repositories;

public class InMemoryUserLibraryRepository : IUserLibraryRepository
{
    private readonly Dictionary<Guid, List<Guid>> _libs = new();

    public Task AddGameToUserAsync(Guid userId, Guid gameId)
    {
        if (!_libs.ContainsKey(userId)) _libs[userId] = new List<Guid>();
        if (!_libs[userId].Contains(gameId)) _libs[userId].Add(gameId);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Guid>> GetUserLibraryAsync(Guid userId)
    {
        if (!_libs.ContainsKey(userId)) return Task.FromResult<IEnumerable<Guid>>(Array.Empty<Guid>());
        return Task.FromResult<IEnumerable<Guid>>(_libs[userId]);
    }
}
