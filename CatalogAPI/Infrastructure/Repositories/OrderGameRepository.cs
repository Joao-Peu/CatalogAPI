using CatalogAPI.Domain.Entities;
using CatalogAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CatalogAPI.Infrastructure.Repositories;

public class OrderGameRepository(CatalogDbContext db) : IOrderGameRepository
{
    public async Task MarkOrderAsProcessedAsync(Guid orderId)
    {
        db.OrderGames.Update(new OrderGame
        {
            Id = orderId,
            IsProcessed = true
        });

        await db.SaveChangesAsync();
    }

    public async Task<bool> ExistsUnprocessedOrder(Guid userId, Guid gameId)
    {
        var exists = await db.OrderGames.AnyAsync(x => x.UserId == userId && x.GameId == gameId && !x.IsProcessed);
        return exists;
    }

    public async Task SaveNewAsync(OrderGame order)
    {
        db.OrderGames.Add(order);
        await db.SaveChangesAsync();
    }
}
