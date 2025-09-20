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

        if (result.Item1 == 3)
        {
            //replay will have ID=0, I'd need to find it in the db otherwise (unnecessary at least for now)
            return Conflict(new { message = "Replay already exists", replay = result.Item2 });
        }

        if (result.Item1 == 1)
        {
            return Ok(new
            {
                message = "Reuploaded replay. Elo not updated.",
                replay = result.Item2
            });
        }

        if (result.Item1 == 2)
        {
            return Ok(new
            {
                message = "Reuploaded replay. Elo updated.",
                replay = result.Item2
            });
        }

        return Ok(new { message = "Successfully uploaded replay", replay = result.Item2 });
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

    [HttpPost("bans")]
    public async Task<IActionResult> Post([FromBody] ReplayBansReport report)
    {
        var service = new ReplaysService(this.config);

        Tuple<int, List<Replay>?> res;
        try
        {
            res = await service.UploadReplayBansReport(report);
        }
        catch (DbUpdateException dbEx)
        {
            Console.WriteLine(dbEx.Message);
            return StatusCode(500, new
            {
                message = "Unexpected error. Couldn't upload bans report."
            });
        }

        if (res.Item1 == 1)
        {
            return NotFound(new
            {
                message = "Host not registered to sodbot."
            });
        }

        if (res.Item1 == 2)
        {
            return NotFound(new
            {
                message = "Guest not registered to sodbot."
            });
        }

        if (res.Item1 == 3)
        {
            return NotFound(new
            {
                message =
                    "Replays corresponding to the report not found. Make sure you are uploading the report after both replays are submitted" +
                    " and in the same channel as they were submitted. It is also possible that the report was already submitted."
            });
        }

        if (res.Item1 == 4)
        {
            return BadRequest(new
            {
                message = "Report is invalid."
            });
        }

        return Ok(new
        {
            message = "Successfully uploaded report.",
            replays = res.Item2
        });
    }
}
    

















