using Microsoft.EntityFrameworkCore;

namespace CatalogAPI.Infrastructure.Repositories;

public class EfUserLibraryRepository : IUserLibraryRepository
{
    private readonly Persistence.CatalogDbContext _db;

    public EfUserLibraryRepository(Persistence.CatalogDbContext db)
    {
        _db = db;
    }

    public async Task AddGameToUserAsync(Guid userId, Guid gameId)
    {
        var exists = await _db.UserLibrary.AnyAsync(x => x.UserId == userId && x.GameId == gameId);
        if (!exists)
        {
            _db.UserLibrary.Add(new Domain.Entities.UserLibraryEntry
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                GameId = gameId
            });
            await _db.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Guid>> GetUserLibraryAsync(Guid userId)
    {
        return await _db.UserLibrary
            .Where(x => x.UserId == userId)
            .Select(x => x.GameId)
            .ToListAsync();
    }
}
