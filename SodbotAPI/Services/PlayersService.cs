using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Versioning;
using SodbotAPI.DB;
using SodbotAPI.DB.Models;
using SodbotAPI.DB.Models.PlayersDtos;
using SodbotAPI.DB.Models.ReplaysDtos;
using Microsoft.EntityFrameworkCore;

namespace SodbotAPI.Services;

public class PlayersService : SodbotService
{
    public PlayersService(IConfiguration config)
    {
        this.Context = new AppDbContext(config);
    }

    public async Task<List<Player>> GetPlayers()
    {
        return await this.Context.Players.ToListAsync();
    }

    public async Task<Player?> GetPlayer(int id)
    {
        return await this.Context.Players.FindAsync(id);
    }

    public async Task<List<Player>> GetPlayersByIds(int[] ids)
    {
        return await this.Context.Players.Where(p => ids.Contains(p.Id)).ToListAsync();
    }
    
    public async Task<Player?> GetPlayerByDiscordId(string discordId)
    {
        return await this.Context.Players.FirstOrDefaultAsync(p => p.DiscordId == discordId);
    }

    private class PlayerElo
    {
        public ReplayPlayerWithPlayer RPWithPlayer { get; set; }

        public int GameCount { get; set; }
    }

    public async Task<List<PlayerWithGameCount>> GetReplayCountsByPlayerIds(int[] ids, Franchise franchise, bool isTeamGame)
    {
        return await this.Context.Replays.Join(this.Context.ReplayPlayers,
                replay => replay.Id,
                repPlay => repPlay.ReplayId,
                (replay, repPlay) => new { replay, repPlay })
            .Where(r => ids.Contains(r.repPlay.PlayerId) && r.replay.Franchise == franchise &&
                        r.replay.IsTeamGame == isTeamGame)
            .GroupBy(r => r.repPlay.PlayerId,
                r => r.replay.Id,
                (pid, rid) => new PlayerWithGameCount()
                {
                    Id = pid,
                    GameCount = rid.Count()
                }
            ).ToListAsync();
    }
    
    public async Task<List<ReplayPlayerWithPlayer>> UpdatePlayersEloUsingGameCount(List<ReplayPlayerWithPlayer> replayPlayers, Franchise franchise)
    {
        var gameCounts = await this.GetReplayCountsByPlayerIds(replayPlayers.Select(p => p.Player.Id).ToArray(),
            franchise, replayPlayers.Count != 2);


        var players = replayPlayers.Select(rp =>
        {
            var gameCount = gameCounts.FirstOrDefault(gc => gc.Id == rp.Player.Id);

            if (gameCount is not null)
            {
                return new PlayerElo()
                {
                    GameCount = gameCount.GameCount,
                    RPWithPlayer = rp
                };
            }
            
            return new PlayerElo()
            {
                GameCount = 0,
                RPWithPlayer = rp
            };
        }).ToList();


        var eloProp = ReplaysService.GetEloProperty(replayPlayers.Count, franchise);
        this.UpdateEloUsingGameCount(players, eloProp);

        this.Context.Players.UpdateRange(players.Select(p => p.RPWithPlayer.Player));
        this.Context.SaveChanges();

        return players.Select(p => p.RPWithPlayer).ToList();
    }

    private IEnumerable<PlayerElo> UpdateEloUsingGameCount(List<PlayerElo> players, PropertyInfo eloProp)
    {
        var ps = players.Where(p => p.RPWithPlayer.UploadReplayPlayerPost.Victory).ToList();
        
        if(ps.Count == 0)
        {
            return players;
        }
        double avgWinElo = ps.Average(p => this.GetEloFromPlayer(p.RPWithPlayer.Player, eloProp)!);

        ps = players.Where(p => !p.RPWithPlayer.UploadReplayPlayerPost.Victory).ToList();
        
        if(ps.Count == 0)
        {
            return players;
        }
        double avgLosElo = ps.Average(p => this.GetEloFromPlayer(p.RPWithPlayer.Player, eloProp)!);
        

        double expectedScoreForWinners = this.GetExpectedScore(avgWinElo, avgLosElo);

        foreach (var player in players)
        {
            int k = 25;

            if (player.GameCount < 10)
            {
                k += 120 - player.GameCount * 12;
            }
            
            double elo = player.RPWithPlayer.UploadReplayPlayerPost.SodbotElo;
            
            player.RPWithPlayer.UploadReplayPlayerPost.OldSodbotElo = elo;
            
            elo += k * (player.RPWithPlayer.UploadReplayPlayerPost.Victory ? 1 - expectedScoreForWinners : 0 - (1 - expectedScoreForWinners));
        
            player.RPWithPlayer.UploadReplayPlayerPost.SodbotElo = elo;    
            eloProp.SetValue(player.RPWithPlayer.Player, elo);
        }

        return players;
    }

    public async Task<IEnumerable<ReplayPlayerWithPlayer>> UpdatePlayersElo(List<ReplayPlayerWithPlayer> players, Franchise franchise)
    { 
        var eloProp = ReplaysService.GetEloProperty(players.Count, franchise);
        this.UpdateElo(players, eloProp);

        this.Context.Players.UpdateRange(players.Select(p => p.Player));
        await this.Context.SaveChangesAsync();

        return players.Select(p => p);
    }
    private IEnumerable<ReplayPlayerWithPlayer> UpdateElo(List<ReplayPlayerWithPlayer> players, PropertyInfo eloProp)
    {
        {
            var ps = players.Where(p => p.UploadReplayPlayerPost.Victory).ToList();
            
            if(ps.Count == 0)
            {
                return players;
            }
            
            double avgWinElo = ps.Average(p => this.GetEloFromPlayer(p.Player, eloProp)!);

            ps = players.Where(p => !p.UploadReplayPlayerPost.Victory).ToList();
        
            if(ps.Count == 0)
            {
                return players;
            }
            
            double avgLosElo = ps.Average(p => this.GetEloFromPlayer(p.Player, eloProp)!);
        

            double expectedScoreForWinners = this.GetExpectedScore(avgWinElo, avgLosElo);

            foreach (var player in players)
            {
                int k = 25;
            
                double elo = player.UploadReplayPlayerPost.SodbotElo;
            
                player.UploadReplayPlayerPost.OldSodbotElo = elo;
            
                elo += k * (player.UploadReplayPlayerPost.Victory ? 1 - expectedScoreForWinners : 0 - (1 - expectedScoreForWinners));
        
                player.UploadReplayPlayerPost.SodbotElo = elo;    
                eloProp.SetValue(player.Player, elo);
            }

            return players;
        }
    }
    private double GetEloFromPlayer(Player player, PropertyInfo eloProp) => (double)eloProp.GetValue(player)!;

    private double GetExpectedScore(double d1, double d2)
    {
        var expected = 1 / (1 + Math.Pow(10, (d2 - d1) / 400));

        return expected;
    }

    public Player? AddPlayer(Player player)
    {
        this.Context.Players.Add(player);

        this.Context.SaveChanges();

        return player;
    }

    public Player? UpdatePlayerDiscordId(int id, PlayerPutDto input)
    {
        var player = this.Context.Players.Find(id);

        if (player is null)
        {
            player = new Player()
            {
                Id = id,
                DiscordId = input.DiscordId,
                Nickname = input.Nickname
            };

            this.AddPlayer(player);
            return player;
        }

        if (player.DiscordId is not null)
        {
            return null;
        }

        player.DiscordId = input.DiscordId;
        this.Context.SaveChanges();

        return player;
    }

    public IEnumerable<PlayerWithRank> GetPlayersWithRank(int? pageSize, int? pageNumber, PropertyInfo eloType)
    {
        var parameter = Expression.Parameter(typeof(Player), "p");
        var property = Expression.Property(parameter, eloType);
        var notNullCheck = Expression.NotEqual(property, Expression.Constant(null));
        var labdaFunc = Expression.Lambda<Func<Player, bool>>(notNullCheck, parameter);

        var orderBy = Expression.Lambda(property, parameter);
        
        IQueryable<Player> query = this.Context.Players
            .Where(labdaFunc);

        query = ApplyOrderByDescending(query, orderBy);
        

        int skipped = 0;
        if(pageNumber is not null && pageSize is not null)
        {
            
            skipped = (pageNumber.Value - 1) * pageSize.Value;
            
            query = query.Skip(skipped).Take(pageSize.Value);
        }
        
        var players = query.ToList()
            .Select((p, index) =>
            {
                
                return new PlayerWithRank()
                {
                    Id = p.Id,
                    DiscordId = p.DiscordId,
                    Name = p.Nickname,
                    Elo = (double)eloType.GetValue(p)!,
                    Rank = index + 1 + skipped
                };
            });


        return players;
    }
    //have no idea what this is lol
    private static IQueryable<Player> ApplyOrderByDescending(IQueryable<Player> query, LambdaExpression orderByLambda)
    {
        var orderByDescendingMethod = typeof(Queryable)
            .GetMethods()
            .Single(m => m.Name == "OrderByDescending" && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(Player), orderByLambda.ReturnType);

        return (IQueryable<Player>)orderByDescendingMethod.Invoke(null, new object[] { query, orderByLambda })!;
    }
    public IEnumerable<PlayerWithRank>? GetPlayerAndSurroundingPlayersRank(int targetId, PropertyInfo eloType)
    {
        var rankedPlayers = this.GetPlayersWithRank(null, null, eloType);
        
        if (rankedPlayers is null)
        {
            return null;
        }

        var player = rankedPlayers.FirstOrDefault(p => p.Id == targetId);
        
        if (player is null)
        {
            return null;
        }

        var lowerBound = player.Rank - 5;
        var upperBound =  player.Rank + 5;

        return rankedPlayers.Where(p => p.Rank >= lowerBound && p.Rank <= upperBound);
    }
    public async Task<PlayerAliasesDto?> GetPlayerWithAliases(int id)
    {
        var aliases = await this.Context.ReplayPlayers.Where(rp => rp.PlayerId == id)
            .GroupBy(rp => rp.Nickname)
            .Select(e => new AliasWithCount()
            {
                Nickname = e.Key,
                Count = e.Count()
            })
            .OrderByDescending(e => e.Count)
            .Take(5)
            .Select(E => E.Nickname)
            .ToListAsync();

        if (aliases.Count == 0)
            return null;
        
        return new PlayerAliasesDto()
        {
            Id = id,
            Aliases = aliases
        };
    }

    private class AliasWithCount
    {
        public string Nickname { get; set; }
        public int Count { get; set; }
    }
    
    
}