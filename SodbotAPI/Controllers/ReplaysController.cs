using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SodbotAPI.DB.Models;
using SodbotAPI.DB.Models.ReplaysDtos;
using SodbotAPI.Services;
using Npgsql;
using Npgsql.Replication.PgOutput;

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
            Console.WriteLine("UploadingReplay");
            
            var tuple = service.AddReplay(input);
            var replay = tuple.Item1;

            if (replay!.SkillLevel == SkillLevel.others)
            {
                replay.ReplayPlayers = tuple.Item2.Select(i => i.ReplayPlayer).ToList();
                
                Console.WriteLine("Others");
                
                return Ok(replay);
            }

            
            var playerService = new PlayersService(this.config);
            
            playerService.UpdatePlayersElo(tuple.Item2, input.Franchise);

            replay.ReplayPlayers = tuple.Item2.Select(i => i.ReplayPlayer).ToList();
            
            replay.ReplayPlayers.ForEach(rp =>
            {
                Console.WriteLine($"{rp.OldSodbotElo} > {rp.SodbotElo} : {rp.SodbotElo - rp.OldSodbotElo}");
            });

            return Ok(replay);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            return Conflict(new {message = "Replay already exists"});
        }
    }
}