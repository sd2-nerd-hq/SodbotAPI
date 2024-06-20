using Microsoft.AspNetCore.Mvc;
using SodbotAPI.DB;
using SodbotAPI.DB.Models;
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
    public IActionResult Get()
    {
        var service = new PlayersService(this.config);
        List<Player> players = service.GetPlayers();

        return Ok(players);
    }
    
    [HttpGet("/gamecount/{id:int}")]
    public IActionResult GetGameCount(int id)
    {
        var service = new PlayersService(this.config);

        var result = service.GetReplayCountByPlayerId(id);

        if (result is null)
        {
            return NotFound("Player not found");
        }

        return Ok(result);
    }
    
    
}