using CatalogAPI.Application.Services;
using CatalogAPI.Domain.Entities;
using CatalogAPI.Infrastructure.Search;
using MassTransit;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CatalogAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController : ControllerBase
{
    private readonly GameService _service;
    private readonly GameCacheService _cacheService;
    private readonly GameSearchService _searchService;
    private readonly IPublishEndpoint _publisher;

    public GamesController(GameService service, GameCacheService cacheService, GameSearchService gameSearchService, IPublishEndpoint publisher)
    {
        _service = service;
        _cacheService = cacheService;
        _searchService = gameSearchService;
        _publisher = publisher;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var g = await _service.GetAsync(id);
        if (g == null) return NotFound();
        return Ok(g);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] Game game)
    {        
        var created = await _service.CreateAsync(game);
        await _searchService.IndexGameAsync(game); // sempre indexar no search
        await _cacheService.InvalidateCacheAsync(); // sempre invalidar cache
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] Game game)
    {
        var existing = await _service.GetAsync(id);
        if (existing == null) return NotFound();        
        game.Id = id;
        await _service.UpdateAsync(game);
        await _searchService.IndexGameAsync(game); // sempre indexar no search
        await _cacheService.InvalidateCacheAsync(); // sempre invalidar cache
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{gameId}/order")]
    [Authorize]
    public async Task<IActionResult> PlaceOrder(Guid gameId)
    {
        var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(user, out var userId))
        {
            return Unauthorized();
        }

        var orderResult = await _service.PlaceOrderAsync(userId, gameId);
        if (!orderResult.IsSuccess)
        {
            return BadRequest(orderResult.Error);
        }   

        return Accepted();
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Query param 'q' é obrigatório");
        var results = await _searchService.SearchAsync(q);
        return Ok(results);
    }
}
