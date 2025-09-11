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
    public async Task<IActionResult> Get()
    {
        var service = new ReplaysService(this.config);

        var replays = await service.GetReplays();
        
        return Ok(new
        {
            message = "Successfully retrieved replays",
            replays
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var service = new ReplaysService(this.config);

        var replay = await service.GetReplay(id);

        if (replay is null)
        {
            return NotFound(new { message = "Replay not found" });
        }
        

        return Ok(new
        {
            message = "Successfully retrieved replay",
            replay
        });
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ReplayPostDto input)
    {
        var service = new ReplaysService(this.config);

        var result = await service.UploadReplay(input);

        if (result.Item1 == 409)
        {
            //replay will have ID=0, I'd need to find it in the db otherwise (unnecessary at least for now)
            return Conflict(new { message = "Replay already exists" , replay = result.Item2 });
        }
        
        return Ok(new {message = "Successfully uploaded replay", replay = result.Item2});
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> Post([FromBody] List<ReplayPostDto> input)
    {
        var service = new ReplaysService(this.config);

        int uploaded = 0;
        int failed = 0;
        int duplicates = 0;

        foreach (var replay in input)
        {
            try
            {
                var result = await service.UploadReplayNoEloDifference(replay);

                if (result.Item1 == 200)
                {
                    uploaded++;
                    continue;
                }

                if (result.Item1 == 409)
                {
                    duplicates++;
                    continue;
                }

                throw new Exception($"Failed to upload replay: {result.Item2}");
            }
            catch (Exception e)
            {
                failed++;
                Console.WriteLine($"Error while bulk uploading:\t {e.Message}");
            }
        }

        return Ok(
            new
            {
                message = "Bulk upload completed",
                uploaded,
                failed,
                duplicates
            });
    }

    [HttpPut("bans")]
    public async Task<IActionResult> Put([FromBody] ReplayPostDto input)
    {
        var service = new ReplaysService(this.config);

        

        return Ok();
    }
}


















