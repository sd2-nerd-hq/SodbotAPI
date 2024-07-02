using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SodbotAPI.DB;
using SodbotAPI.DB.Models;
using SodbotAPI.DB.Models.PlayersDtos;
using SodbotAPI.Services;

namespace SodbotAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class PlayersController : Controller
{
    private readonly IConfiguration config;

    public PlayersController(IConfiguration config)
    {
        this.config = config;
    }

    [HttpGet]
    public IActionResult GetPlayers()
    {
        var service = new PlayersService(this.config);
        List<Player> players = service.GetPlayers();

        return Ok(players);
    }

    [HttpGet("{id:int}")]
    public IActionResult GetPlayerById(int id)
    {
        var service = new PlayersService(this.config);
        var player = service.GetPlayer(id);


        if (player is null)
        {
            return NotFound(new { message = "Player not found" });
        }

        return Ok(player);
    }

    [HttpGet("getPlayersByIds")]
    public IActionResult GetPlayersByIds([FromQuery] int[] ids)
    {
        var service = new PlayersService(this.config);
        var players = service.GetPlayersByIds(ids);

        return Ok(players);
    }

    [HttpGet("gamecount/{id:int}")]
    public IActionResult GetGameCount(int id)
    {
        var service = new PlayersService(this.config);

        var result = service.GetPlayer(id);

        if (result is null)
        {
            return NotFound(new { message = "Player not found" });
        }

        return Ok(result);
    }

    [HttpPut("{id:int}")]
    public IActionResult UpdatePlayer(int id, [FromBody] PlayerPutDto player)
    {
        try
        {
            var service = new PlayersService(this.config);

            var result = service.UpdatePlayerDiscordId(id, player);

            if (result is null)
            {
                return BadRequest(new { message = "Player is already registered" });
            }

            return Ok(result);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            return Conflict(new { message = "Discord ID already registered" });
        }
    }
}