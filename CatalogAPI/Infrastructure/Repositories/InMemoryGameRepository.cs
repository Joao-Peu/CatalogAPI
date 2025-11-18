using CatalogAPI.Domain.Entities;

namespace CatalogAPI.Infrastructure.Repositories;

public class InMemoryGameRepository : IGameRepository
{
    private readonly List<Game> _games = new();

    public InMemoryGameRepository()
    {
        // seed
        _games.Add(new Game { Id = Guid.NewGuid(), Title = "Cyber Adventure", Description = "Futuristic RPG", Price = 49.99M });
        _games.Add(new Game { Id = Guid.NewGuid(), Title = "Space Battles", Description = "Multiplayer space shooter", Price = 29.99M });
    }

    public Task AddAsync(Game game)
    {
        _games.Add(game);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        var g = _games.FirstOrDefault(x => x.Id == id);
        if (g != null) _games.Remove(g);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Game>> GetAllAsync() => Task.FromResult<IEnumerable<Game>>(_games);

    public Task<Game?> GetAsync(Guid id) => Task.FromResult(_games.FirstOrDefault(x => x.Id == id));

    public Task UpdateAsync(Game game)
    {
        var existing = _games.FirstOrDefault(x => x.Id == game.Id);
        if (existing != null)
        {
            existing.Title = game.Title;
            existing.Description = game.Description;
            existing.Price = game.Price;
        }
        return Task.CompletedTask;
    }
}
