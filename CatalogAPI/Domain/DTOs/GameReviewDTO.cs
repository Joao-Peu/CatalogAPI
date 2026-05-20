using MongoDB.Bson;

namespace CatalogAPI.Domain.DTOs
{
    /// <summary>
    /// DTO GameReviewDTO
    /// </summary>
    public class GameReviewDTO
    {
        public int Rating { get; set; }        
        public string Comment { get; set; } = string.Empty;        
    }
}
