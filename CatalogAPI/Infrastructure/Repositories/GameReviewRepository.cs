using CatalogAPI.Domain.Entities;
using MongoDB.Driver;

namespace CatalogAPI.Infrastructure.Repositories
{
    public class GameReviewRepository
    {
        private readonly IMongoCollection<GameReview> _collection;

        public GameReviewRepository(IConfiguration config)
        {
            var client = new MongoClient(config["MongoDB:ConnectionString"]);
            var db = client.GetDatabase("fcg-catalog");
            _collection = db.GetCollection<GameReview>("game-reviews");
        }

        public async Task<List<GameReview>> GetByGameIdAsync(Guid gameId)
            => await _collection.Find(r => r.GameId == gameId).ToListAsync();

        public async Task AddAsync(GameReview review)
            => await _collection.InsertOneAsync(review);

        public async Task<double> GetAverageRatingAsync(Guid gameId)
        {
            var reviews = await GetByGameIdAsync(gameId);
            return reviews.Any() ? reviews.Average(r => r.Rating) : 0;
        }
    }
}
