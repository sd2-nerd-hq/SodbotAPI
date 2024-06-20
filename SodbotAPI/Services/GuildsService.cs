using Microsoft.EntityFrameworkCore;
using SodbotAPI.DB;
using SodbotAPI.DB.Models;
using SodbotAPI.DB.Models.GuildsDtos;

namespace SodbotAPI.Services;

public class GuildsService : SodbotService
{
    public GuildsService(IConfiguration config)
    {
        this.Config = config;
        this.Context = new AppDbContext(this.Config);
    }

    public List<GuildGetDto> GetGuildsWithChannels()
    {
        var guilds = this.Context.Guilds.Include(g => g.Channels).ToList();

        List<GuildGetDto> output = guilds.Select(guild =>
            new GuildGetDto()
            {
                Id = guild.Id,
                Name = guild.Name,
                Channels = guild.Channels.Select(c => new ChannelGetPostDto()
                    { Id = c.Id, Name = c.Name, SkillLevel = c.SkillLevel, PrimaryMode = c.PrimaryMode }).ToList()
            }
        ).ToList();

        return output;
    }

    public Guild? GetGuild(long id)
    {
        var guild = this.Context.Guilds.Include(g => g.Channels).FirstOrDefault(g => g.Id == id);

        return guild;
    }

    public Guild? AddGuild(Guild input)
    {
        this.Context.Guilds.Add(input);
        this.Context.SaveChanges();

        return input;
    }

    public Guild? UpdateGuild(long id, GuildPutDto input)
    {
        var guild = this.Context.Guilds.FirstOrDefault(g => g.Id == id);

        if (guild is null)
        {
            return null;
        }

        guild.Name = input.Name;

        this.Context.SaveChanges();

        return guild;
    }
    
    public Channel? GetChannel(long id)
    {
        var channel = this.Context.Channels.Find(id);

        return channel;
    }
    
    public Channel? AddChannel(Channel input)
    {
        this.Context.Channels.Add(input);
        this.Context.SaveChanges();

        return input;
    }
    
    public Channel? AddChannel(GuildPostDto input)
    {
        var existing = this.Context.Channels.Find(input.Channel.Id);

        if (existing is not null)
        {
            existing.Name = input.Channel.Name;
            existing.SkillLevel = input.Channel.SkillLevel;
            existing.PrimaryMode = input.Channel.PrimaryMode;
            
            this.Context.SaveChanges();
            
            return existing;
        }

        var guild = this.Context.Guilds.Find(input.Id);
        
        if (guild is null)
        {
            guild = new Guild()
            {
                Id = input.Id,
                Name = input.Name,
                Channels = []
            };
            this.Context.Guilds.Add(guild);
        }
        
        var channel = new Channel()
        {
            Id = input.Channel.Id,
            GuildId = guild.Id,
            Name = input.Channel.Name,
            SkillLevel = input.Channel.SkillLevel,
            PrimaryMode = input.Channel.PrimaryMode
        };
        
        this.Context.Channels.Add(channel);
        this.Context.SaveChanges();

        return channel;
    }
    
    public Channel? UpdateChannel(long id, ChannelPutDto input)
    {
        var channel = this.Context.Channels.FirstOrDefault(c => c.Id == id);
        
        if (channel is null)
        {
            return null;
        }
        
        
        var guild = this.Context.Guilds.FirstOrDefault(g => g.Id == channel.GuildId);

        if (guild is null)
        {
            return null;
        }
        
        channel.Name = input.Name;
        channel.SkillLevel = input.SkillLevel;
        channel.PrimaryMode = input.PrimaryMode;

        this.Context.SaveChanges();

        return channel;
    }
}