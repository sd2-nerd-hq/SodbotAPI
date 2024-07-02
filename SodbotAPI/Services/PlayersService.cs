using System.Reflection;
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

    public List<Player> GetPlayers()
    {
        return this.Context.Players.ToList();
    }

    public Player? GetPlayer(int id)
    {
        return this.Context.Players.Find(id);
    }

    public List<Player> GetPlayersByIds(int[] ids)
    {
        return this.Context.Players.Where(p => ids.Contains(p.Id)).ToList();
    }

    private class PlayerElo
    {
        public ReplayPlayerWithPlayer RPWithPlayer { get; set; }

        public int GameCount { get; set; }
    }

    public PlayerWithGameCount? GetReplayCountByPlayerId(int id)
    {
        var result = this.Context.Replays.Join(this.Context.ReplayPlayers,
                replay => replay.Id,
                repPlay => repPlay.ReplayId,
                (replay, repPlay) => new { replay, repPlay })
            .Where(r => r.repPlay.PlayerId == id)
            .GroupBy(r => r.repPlay.PlayerId,
                r => r.replay.Id,
                (pid, rid) => new PlayerWithGameCount()
                {
                    Id = pid,
                    GameCount = rid.Count()
                }
            ).FirstOrDefault();

        if (result is null)
        {
            var player = this.Context.Players.Find(id);

            if (player is null)
            {
                return null;
            }

            result = new PlayerWithGameCount()
            {
                Id = id,
                GameCount = 0
            };
        }

        return result;
    }

    public List<PlayerWithGameCount> GetReplayCountsByPlayerIds(int[] ids, Franchise franchise, bool isTeamGame)
    {
        return this.Context.Replays.Join(this.Context.ReplayPlayers,
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
            ).ToList();
    }

    // public List<Player> UpdatePlayersElo(List<ReplayPlayerDto> replayPlayers, Franchise franchise)
    public List<ReplayPlayerWithPlayer> UpdatePlayersElo(List<ReplayPlayerWithPlayer> replayPlayers,
        Franchise franchise)
    {
        var gameCounts = this.GetReplayCountsByPlayerIds(replayPlayers.Select(p => p.Player.Id).ToArray(),
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
        this.UpdateElo(players, eloProp);

        this.Context.Players.UpdateRange(players.Select(p => p.RPWithPlayer.Player));
        this.Context.SaveChanges();

        return players.Select(p => p.RPWithPlayer).ToList();
    }

    private List<PlayerElo> UpdateElo(List<PlayerElo> players, PropertyInfo eloProp)
    {
        double avgWinElo = players.Where(p => p.RPWithPlayer.ReplayPlayer.Victory).Average(p => this.GetEloFromPlayer(p.RPWithPlayer.Player, eloProp)!);
        double avgLosElo = players.Where(p => !p.RPWithPlayer.ReplayPlayer.Victory).Average(p => this.GetEloFromPlayer(p.RPWithPlayer.Player, eloProp)!);

        double expectedScoreForWinners = this.GetExpectedScore(avgWinElo, avgLosElo);

        foreach (var player in players)
        {
            int k = 25;

            if (player.GameCount < 10)
            {
                k += 12 - player.GameCount * 12;
            }
            
            double elo = player.RPWithPlayer.ReplayPlayer.SodbotElo;
            
            player.RPWithPlayer.ReplayPlayer.OldSodbotElo = elo;
            
            elo += k * (player.RPWithPlayer.ReplayPlayer.Victory ? 1 - expectedScoreForWinners : 0 - (1 - expectedScoreForWinners));
        
            player.RPWithPlayer.ReplayPlayer.SodbotElo = elo;    
            eloProp.SetValue(player.RPWithPlayer.Player, elo);
        }

        return players;
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

    // public List<PlayerWithRank>? GetPlayerAndSurroundingPlayersRank()
    // {
    //
    //
    //     
    // }
}