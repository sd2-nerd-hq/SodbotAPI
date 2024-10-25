using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SodbotAPI.DB;
using SodbotAPI.DB.Models;
using SodbotAPI.DB.Models.ReplaysDtos;


namespace SodbotAPI.Services;

public class ReplaysService : SodbotService
{
    private IConfiguration config;
    public ReplaysService(IConfiguration config)
    {
        this.Context = new AppDbContext(config);
        this.config = config;
    }

    public async Task<object> UploadReplay(ReplayDto input)
    {
        try
        {
            var tuple = await this.AddReplay(input);
            var replay = tuple.Item1;

            if (replay!.SkillLevel == SkillLevel.others)
            {
                replay.ReplayPlayers = tuple.Item2.Select(i => i.ReplayPlayer).ToList();

                return replay;
            }

            
            var playerService = new PlayersService(this.config);
            
            await playerService.UpdatePlayersElo(tuple.Item2, input.Franchise);

            replay.ReplayPlayers = tuple.Item2.Select(i => i.ReplayPlayer).ToList();

            return replay;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            return "Replay already exists";
        }
        
    }
    
    public List<Replay> GetReplays()
    {
        var replays = this.Context.Replays.ToList();

        return replays;
    }

    public IEnumerable<Replay> GetReplaysWithPlayers()
    {
        var replays = this.Context.Replays.Include(r => r.ReplayPlayers).ToList();

        return replays;
    }

    public Replay? GetReplay(int id)
    {
        return this.Context.Replays.Include(r => r.ReplayPlayers).FirstOrDefault(r => r.Id == id);
    }
    
    public Replay? GetReplay(string sessionId)
    {
        return this.Context.Replays.Include(r => r.ReplayPlayers).FirstOrDefault(r => r.SessionId == sessionId);
    }

    public async Task<(ReplayWithOldElo?, List<ReplayPlayerWithPlayer>)> AddReplay(ReplayDto input, bool immediateSave = true)
    {
        //gets the type of elo to add if the player's not registered yet
        var eloProp = GetEloProperty(input.ReplayPlayers.Count, input.Franchise);

        var ids = input.ReplayPlayers.Select(p => p.PlayerId);

        var players = await this.Context.Players.Where(p => ids.Any(id => p.Id == id)).ToListAsync();

        List<ReplayPlayerWithPlayer> rpPlayers = new(input.ReplayPlayers.Count);

        input.ReplayPlayers.ForEach(player =>
        {
            var existingPlayer = players.FirstOrDefault(p => p.Id == player.PlayerId);
            
            if (existingPlayer is null)
            {
                existingPlayer = new Player()
                {
                    Id = player.PlayerId,
                    Nickname = player.Nickname,
                    SdElo = null,
                    SdTeamGameElo = null,
                    WarnoElo = null,
                    WarnoTeamGameElo = null
                };
                eloProp.SetValue(existingPlayer, 1200 + (player.Elo - 1200) / 5);

                this.Context.Players.Add(existingPlayer);
            }
            else if (eloProp.GetValue(existingPlayer) is null)
            {
                eloProp.SetValue(existingPlayer, 1200 + (player.Elo - 1200) / 5);
            }

            rpPlayers.Add(new ReplayPlayerWithPlayer()
            {
                ReplayPlayer = new ReplayPlayerWithEloDifference(player, existingPlayer.DiscordId,
                    (double)eloProp.GetValue(existingPlayer)!),
                Player = existingPlayer
            });

        });


        var replayType = input.ReplayType;

        if (replayType is null)
        {
            var channel = await this.Context.Channels.FindAsync(input.UploadedIn);

            replayType = channel?.SkillLevel ?? SkillLevel.others;
        }

        Replay replay = new()
        {
            Id = 0,
            SessionId = input.SessionId,
            UploadedIn = input.UploadedIn,
            UploadedBy = input.UploadedBy,
            UploadedAt = input.UploadedAt,
            Franchise = input.Franchise,
            Version = input.Version,
            IsTeamGame = input.IsTeamGame,
            Map = input.Map,
            MapType = input.MapType,
            VictoryCondition = input.VictoryCondition,
            DurationSec = input.DurationSec,
            SkillLevel = replayType.Value,
        };
        
        
        var replayPlayers = input.ReplayPlayers.Select(r => new ReplayPlayer()
        {
            PlayerId = r.PlayerId,
            Nickname = r.Nickname,
            Elo = r.Elo,
            MapSide = r.MapSide,
            Victory = r.Victory,
            Division = r.Division,
            Income = r.Income,
            DeckCode = r.DeckCode
        }).ToList();

        replay.ReplayPlayers = replayPlayers;

        this.Context.Replays.Add(replay);

        if (immediateSave)
            await this.Context.SaveChangesAsync();
        
        var output = new ReplayWithOldElo(replay, new List<ReplayPlayerWithEloDifference>(rpPlayers.Count));

        return (output, rpPlayers);
    }
    
    
    
    public async Task<int> SaveChangesAsync() => await this.Context.SaveChangesAsync();

    public static PropertyInfo GetEloProperty(int playerCount, Franchise franchise)
    {
        bool isTeamGame = playerCount > 2;

        //returns property name depending on the franchise and if it's a team game (number of players > 2)
        var eloPropName = franchise == Franchise.sd2
            ? (isTeamGame ? "SdTeamGameElo" : "SdElo")
            : (isTeamGame ? "WarnoTeamGameElo" : "WarnoElo");


        //no need for nullable type, will always be found
        return ReplaysService.GetEloProperty(eloPropName)!;
    }
    
    public static PropertyInfo? GetEloProperty(string propName)
    {
        var playerType = typeof(Player);
        
        return playerType.GetProperty(propName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
    }
}