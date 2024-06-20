using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SodbotAPI.DB.Models;
using SodbotAPI.DB.Models.ReplaysDtos;
using SodbotAPI.Services;
using Npgsql;

namespace SodbotAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class ReplaysController : Controller
{
    private readonly IConfiguration config;
    public ReplaysController(IConfiguration config)
    {
        this.config = config;
    }
    [HttpGet]
    public IActionResult Get()
    {
        var service = new ReplaysService(this.config);

        var replays = service.GetReplaysWithPlayers();

        return Ok(replays);
    }

    [HttpPost]
    public IActionResult Post([FromBody] ReplayDto input)
    {
        var service = new ReplaysService(this.config);
        
        try
        {
            var replay = service.AddReplay(input);

            if (replay!.SkillLevel == SkillLevel.others)
                return Ok(replay);

            
            var playerService = new PlayersService(this.config);
            
            playerService.UpdatePlayersElo(input.ReplayPlayers, input.Franchise);

            return Ok(replay);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            return Conflict("Replay already exists");
        }
    }
}