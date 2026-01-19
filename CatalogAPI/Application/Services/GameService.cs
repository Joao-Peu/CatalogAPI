using CatalogAPI.Application.Abstractions;
using CatalogAPI.Domain.Entities;
using CatalogAPI.Infrastructure.Repositories;
using MassTransit;
using Shared.Events;

namespace CatalogAPI.Application.Services;

public class GameService(IGameRepository gameRepo, IUserLibraryRepository libraryRepo, IOrderGameRepository orderGameRepository, IPublishEndpoint publisher, ILogger<GameService> logger)
{
    public async Task<IEnumerable<Game>> GetAllAsync() => await gameRepo.GetAllAsync();

    public async Task<Game?> GetAsync(Guid id) => await gameRepo.GetAsync(id);

    public async Task<Game> CreateAsync(Game game)
    {
        game.Id = Guid.NewGuid();
        await gameRepo.AddAsync(game);
        return game;
    }

    public async Task UpdateAsync(Game game) => await gameRepo.UpdateAsync(game);

    public async Task DeleteAsync(Guid id) => await gameRepo.DeleteAsync(id);

    public async Task<Result> PlaceOrderAsync(Guid userId, Guid gameId)
    {
        var game = await gameRepo.GetAsync(gameId);
        if (game == null)
        {
            return new Error("game_not_found", "O jogo especificado não existe.");
        }

        var existsGameToUser = await libraryRepo.ExistsGameToUserAsync(userId, gameId);
        if  (existsGameToUser)
        {
            return new Error("game_already_owned", "O usuário já possui este jogo em sua biblioteca.");
        }

        var existsOrder = await orderGameRepository.ExistsUnprocessedOrder(userId, gameId);
        if (existsOrder)
        {
            return new Error("order_game_not_processed", "Existe um pedido de jogo em processamento. Aguarde o processamento ser concluído.");
        }

        var orderGame = new OrderGame
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GameId = gameId,
            IsProcessed = false
        };

        await orderGameRepository.SaveNewAsync(orderGame);

        var evt = new OrderPlacedEvent(orderGame.Id, orderGame.UserId, orderGame.GameId, game.Price);
        await publisher.Publish(evt);

        return Result.Success();
    }

    public async Task HandlePaymentProcessedAsync(PaymentProcessedEvent e)
    {
        await orderGameRepository.MarkOrderAsProcessedAsync(e.OrderId);
        if (e.Status == "Approved")
        {
            await libraryRepo.AddGameToUserAsync(e.UserId, e.GameId);
        }
        else
        {
            logger.LogInformation("O pagamento do pedido {OrderId} foi reprovado.", e.OrderId);
        }
    }
}
