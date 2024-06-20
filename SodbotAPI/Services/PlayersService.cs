using System.Reflection;
using SodbotAPI.DB;
using SodbotAPI.DB.Models;
using SodbotAPI.DB.Models.PlayersDtos;
using SodbotAPI.DB.Models.ReplaysDtos;

namespace SodbotAPI.Services;

public class PlayersService : SodbotService
{
    public PlayersService(IConfiguration config)
    {
        this.Config = config;
        this.Context = new AppDbContext(this.Config);
    }

    public List<Player> GetPlayers()
    {
        return this.Context.Players.ToList();
    }

    public Player? GetPlayer(int id)
    {
        return this.Context.Players.Find(id);
    }

    private class PlayerElo
    {
        public Player player { get; set; }

        public bool Victory { get; set; }

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
        var result = this.Context.Replays.Join(this.Context.ReplayPlayers,
                replay => replay.Id,
                repPlay => repPlay.ReplayId,
                (replay, repPlay) => new { replay, repPlay })
            .Where(r => ids.Contains(r.repPlay.PlayerId) && r.replay.Franchise == franchise && r.replay.IsTeamGame == isTeamGame)
            .GroupBy(r => r.repPlay.PlayerId,
                r => r.replay.Id,
                (pid, rid) => new PlayerWithGameCount()
                {
                    Id = pid,
                    GameCount = rid.Count()
                }
            ).ToList();

        if (result.Count >= ids.Length)
        {
            return result;
        }

        foreach (var id in ids)
        {
            if (result.Any(r => r.Id == id))
                continue;


            var player = this.Context.Players.Find(id);

            if (player is null)
            {
                continue;
            }

            result.Add(new PlayerWithGameCount()
            {
                Id = id,
                GameCount = 0
            });
        }

        return result;
    }

    public List<Player> UpdatePlayersElo(List<ReplayPlayerDto> replayPlayers, Franchise franchise)
    {
        List<PlayerElo> players = [];

        //not really proud of this one, but there's max 20 players, so it's not like the complexity matters
        foreach (var replayPlayer in replayPlayers)
        {
            //should never be null, since it's called only after replay upload (which creates players if they don't exist)
            var player = this.Context.Players.Find(replayPlayer.PlayerId);

            players.Add(new PlayerElo()
            {
                player = player!,
                Victory = replayPlayer.Victory
            });
        }

        var gameCounts = this.GetReplayCountsByPlayerIds(players.Select(p => p.player.Id).ToArray(), 
                                                                                franchise, replayPlayers.Count != 2);

        foreach (var player in players)
        {
            player.GameCount = gameCounts.FirstOrDefault(g => g.Id == player.player.Id)!.GameCount;
        }

        var eloProp = ReplaysService.GetEloProperty(replayPlayers.Count, franchise);
        this.UpdateElo(players, eloProp);
        
        this.Context.Players.UpdateRange(players.Select(p => p.player));
        this.Context.SaveChanges();

        return players.Select(p => p.player).ToList();
    }

    private List<PlayerElo> UpdateElo(List<PlayerElo> players, PropertyInfo eloProp)
    {
        double avgWinElo = players.Where(p => p.Victory).Average(p => this.GetEloFromPlayer(p.player, eloProp)!);
        double avgLosElo = players.Where(p => !p.Victory).Average(p => this.GetEloFromPlayer(p.player, eloProp)!);

        double expectedScoreForWinners = this.GetExpectedScore(avgWinElo, avgLosElo);

        foreach (var player in players)
        {
            int k = 25;

            if (player.GameCount < 10)
            {
                k += 80 - player.GameCount * 8;
            }

            double elo = this.GetEloFromPlayer(player.player, eloProp);
            
            eloProp.SetValue(player.player, elo + k * (player.Victory ? 1 - expectedScoreForWinners : 0 - (1 - expectedScoreForWinners)));
        }
        return players;
    }

    private double GetEloFromPlayer(Player player, PropertyInfo eloProp) => (double)eloProp.GetValue(player)!;

    private double GetExpectedScore(double d1, double d2)
    {
        var expected = 1 / (1 + Math.Pow(10, (d2 - d1) / 400));

        return expected;
    }
}