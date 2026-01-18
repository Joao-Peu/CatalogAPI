using CatalogAPI.Application.Abstractions;
using CatalogAPI.Domain.Entities;
using CatalogAPI.Infrastructure.Repositories;
using CatalogAPI.Shared.Events;
using MassTransit;
using Shared.Events;

namespace CatalogAPI.Application.Services;

public class GameService
{
    private readonly IGameRepository _gameRepo;
    private readonly IUserLibraryRepository _libraryRepo;
    private readonly IPublishEndpoint _publisher;

    public GameService(IGameRepository gameRepo, IUserLibraryRepository libraryRepo, IPublishEndpoint publisher)
    {
        _gameRepo = gameRepo;
        _libraryRepo = libraryRepo;
        _publisher = publisher;
    }

    public async Task<IEnumerable<Game>> GetAllAsync() => await _gameRepo.GetAllAsync();

    public async Task<Game?> GetAsync(Guid id) => await _gameRepo.GetAsync(id);

    public async Task<Game> CreateAsync(Game game)
    {
        game.Id = Guid.NewGuid();
        await _gameRepo.AddAsync(game);
        return game;
    }

    public async Task UpdateAsync(Game game) => await _gameRepo.UpdateAsync(game);

    public async Task DeleteAsync(Guid id) => await _gameRepo.DeleteAsync(id);

    public async Task<Result> PlaceOrderAsync(Guid userId, Guid gameId)
    {
        var game = await _gameRepo.GetAsync(gameId);
        if (game == null)
        {
            return new Error("game_not_found", "O jogo especificado não existe.");
        }

        var evt = new OrderPlacedEvent(userId, gameId, game.Price);
        await _publisher.Publish(evt);

        return Result.Success();
    }

    public async Task HandlePaymentProcessedAsync(PaymentProcessedEvent e)
    {
        if (e.Status == "Approved")
        {
            await _libraryRepo.AddGameToUserAsync(e.UserId, e.GameId);
        }
        else
        {
            // for rejected, we might log or take other actions
        }
    }
}
