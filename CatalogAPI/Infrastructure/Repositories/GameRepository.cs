using CatalogAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CatalogAPI.Infrastructure.Repositories;

public class GameRepository(Persistence.CatalogDbContext db) : IGameRepository
{
    public async Task AddAsync(Game game)
    {
        db.Games.Add(game);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var g = await db.Games.FirstOrDefaultAsync(x => x.Id == id);
        if (g != null)
        {
            db.Games.Remove(g);
            await db.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Game>> GetAllAsync()
        => await db.Games.AsNoTracking().ToListAsync();

    public async Task<Game?> GetAsync(Guid id)
        => await db.Games.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

    public async Task UpdateAsync(Game game)
    {
        db.Games.Update(game);
        await db.SaveChangesAsync();
    }
}
