using CatalogAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CatalogAPI.Infrastructure.Repositories;

public class EfGameRepository : IGameRepository
{
    private readonly Persistence.CatalogDbContext _db;

    public EfGameRepository(Persistence.CatalogDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Game game)
    {
        _db.Games.Add(game);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var g = await _db.Games.FirstOrDefaultAsync(x => x.Id == id);
        if (g != null)
        {
            _db.Games.Remove(g);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Game>> GetAllAsync()
        => await _db.Games.AsNoTracking().ToListAsync();

    public async Task<Game?> GetAsync(Guid id)
        => await _db.Games.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

    public async Task UpdateAsync(Game game)
    {
        _db.Games.Update(game);
        await _db.SaveChangesAsync();
    }
}
