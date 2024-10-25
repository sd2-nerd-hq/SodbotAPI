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
    public async Task<IActionResult> Post([FromBody] ReplayDto input)
    {
        var service = new ReplaysService(this.config);
        ReplayWithOldElo? replay = null;
        try
        {
            var tuple = await service.AddReplay(input, false);
            replay = tuple.Item1;
            await service.SaveChangesAsync();

            if (replay!.SkillLevel == SkillLevel.others)
            {
                replay.ReplayPlayers = tuple.Item2.Select(i => i.ReplayPlayer).ToList();

                return Ok(replay);
            }


            var playerService = new PlayersService(this.config);

            await playerService.UpdatePlayersElo(tuple.Item2, input.Franchise);

            replay.ReplayPlayers = tuple.Item2.Select(i => i.ReplayPlayer).ToList();

            return Ok(replay);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            Replay? ogReplay = service.GetReplay(input.SessionId);

            List<ReplayPlayerWithEloDifference> players = new();
            ogReplay?.ReplayPlayers.ForEach(rp => players.Add(new ReplayPlayerWithEloDifference()));

            replay = new ReplayWithOldElo(ogReplay, );

            if (ogReplay is null)
            {
                return StatusCode(500);
            }
            
            
            return Conflict(new { message = "Replay already exists" , replay });
        }
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> Post([FromBody] List<ReplayDto> input)
    {
        var service = new ReplaysService(this.config);

        int uploaded = 0;
        int failed = 0;
        int duplicates = 0;

        foreach (var replay in input)
        {
            try
            {
                var result = await service.UploadReplay(replay);

                if (result is string)
                {
                    duplicates++;
                    continue;
                }
                
                // ReplayWithOldElo replayRes = (ReplayWithOldElo)result;
                
                uploaded++;
            }
            catch (Exception e)
            {
                failed++;
            }
        }
        
        return Ok(
        new {
            uploaded,
            failed,
            duplicates = 0
        });
    }
}