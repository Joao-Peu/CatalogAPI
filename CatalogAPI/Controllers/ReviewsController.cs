using CatalogAPI.Domain.DTOs;
using CatalogAPI.Domain.Entities;
using CatalogAPI.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogAPI.Controllers;

[ApiController]
[Route("api/games/{gameId}/reviews")]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly GameReviewRepository _reviewRepo;

    /// <summary>
    /// Retorna todas as avaliações de um jogo específico, identificando-o pelo gameId. Cada avaliação inclui a nota, comentário e o nome do usuário que fez a avaliação.
    /// </summary>
    /// <param name="gameId"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> Get(Guid gameId)
        => Ok(await _reviewRepo.GetByGameIdAsync(gameId));

    [HttpPost]
    public async Task<IActionResult> Post(Guid gameId, [FromBody] GameReviewDTO dto)
    {
        var review = new GameReview
        {
            GameId = gameId,
            UserId = Guid.Parse(User.FindFirst("sub")!.Value),
            UserName = User.FindFirst("name")?.Value ?? "Anonymous",
            Rating = dto.Rating,
            Comment = dto.Comment
        };
        await _reviewRepo.AddAsync(review);
        return Created($"api/games/{gameId}/reviews", review);
    }
}
