using MongoDB.Bson;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CatalogAPI.Domain.Entities
{
    public class GameReview
    {
        public ObjectId Id { get; set; }
        public Guid GameId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int Rating { get; set; }        // 1-5
        public string Comment { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new(); // flexível
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
