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

        return Ok(guilds);
    }
    
    [HttpGet("{id:long}")]
    public IActionResult Get(long id)
    {
        var service = new GuildsService(this.config);

        var guild = service.GetGuild(id);

        if (guild is null)
        {
            return NotFound("Guild doesn't exist.");
        }

        return Ok(guild);
    }
    
    [HttpPost]
    public IActionResult Post([FromBody] Guild input)
    {
        var service = new GuildsService(this.config);

        var guild = service.AddGuild(input);

        return Ok(guild!);
    }
    
    [HttpPut("{id:long}")]
    public IActionResult Put(long id, [FromBody] GuildPutDto input)
    {
        var service = new GuildsService(this.config);

        var guild = service.UpdateGuild(id, input);

        if (guild is null)
        {
            return NotFound("Guild doesn't exist.");
        }

        return Ok(guild);
    }
    
    [HttpGet("channels/{id:long}")]
    public IActionResult GetChannel(long id)
    {
        var service = new GuildsService(this.config);

        var channel = service.GetChannel(id);

        if (channel is null)
        {
            return NotFound("Channel doesn't exist.");
        }

        return Ok(channel);
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
    //         return NotFound("Channel doesn't exist.");
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
            return NotFound("Channel doesn't exist.");
        }
        
        return Ok(channel);
    }
    
    [HttpPut("channels/{id:long}")]
    public IActionResult Put(long id, [FromBody] ChannelPutDto input)
    {
        var service = new GuildsService(this.config);

        var channel = service.UpdateChannel(id, input);
        
        if (channel is null)
        {
            return NotFound("Channel doesn't exist.");
        }
        
        return Ok(channel);
    }
}