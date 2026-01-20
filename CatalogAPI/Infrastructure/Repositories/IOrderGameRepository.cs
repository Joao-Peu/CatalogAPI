using CatalogAPI.Domain.Entities;

namespace CatalogAPI.Infrastructure.Repositories;

public interface IOrderGameRepository
{
    Task<bool> ExistsUnprocessedOrder(Guid userId, Guid gameId);
    Task MarkOrderAsProcessedAsync(Guid orderId);
    Task SaveNewAsync(OrderGame order);
}
