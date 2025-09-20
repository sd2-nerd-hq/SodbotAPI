using Microsoft.AspNetCore.Http.HttpResults;
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
    public async Task<IActionResult> GetPlayers()
    {
        var service = new PlayersService(this.config);
        List<Player> players = await service.GetPlayers();

        return Ok(new {
            message = "Successfully retrieved players",
            players
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetPlayerById(int id)
    {
        var service = new PlayersService(this.config);
        var player = await service.GetPlayer(id);


        if (player is null)
        {
            return NotFound(new { message = "Player not found" });
        }

        return Ok(new
        {
            message = "Successfully retrieved player",
            player
        });
    }
    
    [HttpGet("byDiscordId/{id}")]
    public async Task<IActionResult> GetPlayerById(string id)
    {
        var service = new PlayersService(this.config);
        var player = await service.GetPlayer(id);


        if (player is null)
        {
            return NotFound(new { message = "Player not found" });
        }

        return Ok(new
        {
            message = "Successfully retrieved player",
            player
        });
    }
    [HttpGet("guessPlayerFromDiscordId/{discordId}")]
    public async Task<IActionResult> GuessEugenId(string discordId)
    {
        var service = new PlayersService(this.config);
        var player = await service.GetPlayerByDiscordId(discordId);

        if (player is not null)
        {
            return StatusCode(208, new
            {
                message = "Player already registered.",
                player
            });
        }

        var ret = await service.GuessPlayersIdFromUploads(discordId);

        if (ret.Item1 > 0)
        {
            return NotFound(new { message = "Results inconclusive." });
        }
        
        player = await service.GetPlayer(ret.Item2);

        return Ok(new
        {
            message = "Successfully guessed player",
            player
        });
    }

    [HttpGet("aliases/{id:int}")]
    public async Task<IActionResult> GetPlayerAliasesById(int id)
    {
        var service = new PlayersService(this.config);
        
        var player = await service.GetPlayerWithAliases(id);
        
        if (player is null)
        {
            return NotFound(new { message = "Player not found" });
        }
        
        return Ok(new
        {
            message = "Successfully retrieved player aliases",
            player
        });
    }

    [HttpGet("getPlayersByIds")]
    public async Task<IActionResult> GetPlayersByIds([FromQuery] int[] ids)
    {
        var service = new PlayersService(this.config);
        var players = await service.GetPlayersByIds(ids);

        return Ok(new {
            message = "Successfully retrieved players",
            players
        });
    }

    [HttpGet("gamecount/{id:int}")]
    public async Task<IActionResult> GetGameCount(int id)
    {
        var service = new PlayersService(this.config);

        var result = await service.GetPlayer(id);

        if (result is null)
        {
            return NotFound(new { message = "Player not found" });
        }

        return Ok(new
        {
            message = "Successfully retrieved player's game count",
            player = result
        });
    }
    [HttpGet("rank")]
    public IActionResult GetPlayersWithRank(int? pageSize = null, int? pageNumber = null, string eloType = "SdElo")
   {
        var service = new PlayersService(this.config);
        
        var eloProp = ReplaysService.GetEloProperty(eloType);
        
        if (eloProp is null)
        {
            return BadRequest(new { message = "Invalid elo type" });
        }

        var result = service.GetPlayersWithRank(pageSize, pageNumber, eloProp);

        return Ok(new
        { 
            message = "Successfully retrieved players with rank",
            players = result
        });
    }
    
    [HttpGet("rank/{id}")]
    public async Task<IActionResult> GetSurroundingPlayersWithRank(string id,  string eloType = "SdElo")
    {
        var service = new PlayersService(this.config);

        //check if ID is eugen ID or discord ID

        Player? player;
        string? message;
        if (id.Length <= 8)
        {
            player = await service.GetPlayer(Convert.ToInt32(id));
            message = "Player not found";
        }
        else
        {
            player =  await service.GetPlayerByDiscordId(id);
            message = "Player with given Discord ID isn't registered";
            
        }

        if (player is null)
        {
            return NotFound(new { message });
        }
        
        var eloProp = ReplaysService.GetEloProperty(eloType);

        if (eloProp is null)
        {
            return BadRequest(new { message = "Invalid elo type" });
        }

        var result = service.GetPlayerAndSurroundingPlayersRank(player.Id, eloProp);
        
        if (result is null)
        {
            return NotFound(new { message = "Player doesn't have elo yet" });
        }

        return Ok(new
        {
            message = "Successfully retrieved player and surrounding players",
            players = result
        });
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

            return Ok(new
            {
                message = "Successfully updated player",
                player = result
            });
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            return Conflict(new { message = "Discord ID already registered" });
        }
    }
}