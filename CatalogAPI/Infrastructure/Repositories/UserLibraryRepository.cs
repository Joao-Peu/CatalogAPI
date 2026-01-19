using CatalogAPI.Domain.Entities;
using CatalogAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CatalogAPI.Infrastructure.Repositories;

public class UserLibraryRepository(CatalogDbContext db) : IUserLibraryRepository
{
    public async Task AddGameToUserAsync(Guid userId, Guid gameId)
    {
        db.UserLibraries.Add(new UserLibraryEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GameId = gameId
        });
        await db.SaveChangesAsync();
    }

    public async Task<bool> ExistsGameToUserAsync(Guid userId, Guid gameId)
    {
        var exists = await db.UserLibraries.AnyAsync(x => x.UserId == userId && x.GameId == gameId);
        return exists;
    }

    public async Task<IEnumerable<Guid>> GetUserLibraryAsync(Guid userId)
    {
        return await db.UserLibraries
            .Where(x => x.UserId == userId)
            .Select(x => x.GameId)
            .ToListAsync();
    }
}
