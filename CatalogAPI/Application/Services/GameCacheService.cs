using CatalogAPI.Domain.Entities;
using CatalogAPI.Infrastructure.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace CatalogAPI.Application.Services
{
    public class GameCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly IGameRepository _repo;
        private const string ALL_GAMES_KEY = "all_games";
        private static readonly TimeSpan TTL = TimeSpan.FromMinutes(5);

        public GameCacheService(IDistributedCache cache, IGameRepository repo)
        {
            _cache = cache;
            _repo = repo;
        }

        public async Task<IEnumerable<Game>> GetAllGamesAsync()
        {
            var cached = await _cache.GetStringAsync(ALL_GAMES_KEY);
            if (cached != null)
                return JsonSerializer.Deserialize<IEnumerable<Game>>(cached)!;

            var games = await _repo.GetAllAsync();
            await _cache.SetStringAsync(ALL_GAMES_KEY,
                JsonSerializer.Serialize(games),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TTL });
            return games;
        }

        public async Task InvalidateCacheAsync()
            => await _cache.RemoveAsync(ALL_GAMES_KEY);
    }
}
