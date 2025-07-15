using Microsoft.AspNetCore.Mvc;
using SodbotAPI.Services;
using SodbotAPI.DB.Models;
using SodbotAPI.DB.Models.GuildsDtos;

namespace SodbotAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class GuildsController : Controller
{
    private readonly IConfiguration config;
    public GuildsController(IConfiguration config)
    {
        this.config = config;
    }
    
    [HttpGet]
    public IActionResult Get()
    {
        var service = new GuildsService(this.config);

        var guilds = service.GetGuildsWithChannels();

        return Ok(new
        {
            message = "Successfully retrieved guilds",
            guilds
        });
    }
    
    [HttpGet("{id}")]
    public IActionResult Get(string id)
    {
        var service = new GuildsService(this.config);

        var guild = service.GetGuild(id);

        if (guild is null)
        {
            return NotFound(new {message = "Guild doesn't exist"});
        }

        return Ok(new
        {
            message = "Successfully retrieved guild",
            guild
        });
    }
    
    [HttpPost]
    public IActionResult Post([FromBody] Guild input)
    {
        var service = new GuildsService(this.config);

        var guild = service.AddGuild(input);

        return Ok(new
        {
            message = "Successfully added guild",
            guild
        });
    }
    
    [HttpPut("{id}")]
    public IActionResult Put(string id, [FromBody] GuildPutDto input)
    {
        var service = new GuildsService(this.config);

        var guild = service.UpdateGuild(id, input);

        if (guild is null)
        {
            return NotFound(new {message ="Guild doesn't exist"});
        }

        return Ok(new
        {
            message = "Successfully updated guild",
            guild
        });
    }
    
    [HttpGet("channels/{id}")]
    public IActionResult GetChannel(string id)
    {
        var service = new GuildsService(this.config);

        var channel = service.GetChannel(id);

        if (channel is null)
        {
            return NotFound(new {message ="Channel doesn't exist."});
        }

        return Ok(new {
            message = "Successfully retrieved channel",
            channel
        });
    }
    
    // [HttpPost("channels")]
    // public IActionResult Post([FromBody] Channel input)
    // {
    //     var service = new GuildsService(this.config);
    //
    //     var channel = service.AddChannel(input);
    //     
    //     if (channel is null)
    //     {
    //         return NotFound(new {message = "Channel doesn't exist."});
    //     }
    //     
    //     return Ok(channel);
    // }
    
    [HttpPost("channels")]
    public IActionResult Post([FromBody] GuildPostDto input)
    {
        var service = new GuildsService(this.config);

        var channel = service.AddChannel(input);
        
        if (channel is null)
        {
            return NotFound(new {message = "Channel doesn't exist"});
        }
        
        return Ok(new
        {
            message = "Successfully added channel",
            channel
        });
    }
    
    [HttpPut("channels/{id}")]
    public IActionResult Put(string id, [FromBody] ChannelPutDto input)
    {
        var service = new GuildsService(this.config);

        var channel = service.UpdateChannel(id, input);
        
        if (channel is null)
        {
            return NotFound(new {message = "Channel doesn't exist"});
        }
        
        return Ok(new
        {
            message = "Successfully updated channel",
            channel
        });
    }
}