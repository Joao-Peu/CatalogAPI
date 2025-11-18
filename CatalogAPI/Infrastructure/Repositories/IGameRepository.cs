using CatalogAPI.Domain.Entities;

namespace CatalogAPI.Infrastructure.Repositories;

public interface IGameRepository
{
    Task<IEnumerable<Game>> GetAllAsync();
    Task<Game?> GetAsync(Guid id);
    Task AddAsync(Game game);
    Task UpdateAsync(Game game);
    Task DeleteAsync(Guid id);
}
