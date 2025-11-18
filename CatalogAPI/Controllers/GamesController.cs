using CatalogAPI.Application.Services;
using CatalogAPI.Domain.Entities;
using CatalogAPI.Domain.Events;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController : ControllerBase
{
    private readonly GameService _service;
    private readonly IPublishEndpoint _publisher;

    public GamesController(GameService service, IPublishEndpoint publisher)
    {
        _service = service;
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
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/order")]
    [Authorize]
    public async Task<IActionResult> PlaceOrder(Guid id, [FromQuery] Guid userId)
    {
        await _service.PlaceOrderAsync(userId, id);
        return Accepted();
    }
}
