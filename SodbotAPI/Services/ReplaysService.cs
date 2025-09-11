using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SodbotAPI.DB;
using SodbotAPI.DB.Models;
using SodbotAPI.DB.Models.ReplaysDtos;
using SodbotAPI.DB.Models.ReplaysDtos.ReplayPlayers;


namespace SodbotAPI.Services;

public class ReplaysService : SodbotService
{
    private IConfiguration config;

    public ReplaysService(IConfiguration config)
    {
        this.Context = new AppDbContext(config);
        this.config = config;
    }

    public async Task<IEnumerable<Replay>> GetReplays()
    {
        return await this.Context.Replays.Include(r => r.ReplayPlayers).ToListAsync();
    }

    public async Task<List<ReplayGetDto>> GetReplaysWithPlayers()
    {
        return await this.Context.Replays.GroupJoin(this.Context.ReplayPlayers
                .Join(this.Context.Players, rp => rp.PlayerId, p => p.Id, (rp, p) => new { rp, p }),
            r => r.Id, rp => rp.rp.PlayerId,
            (replay, rpTuple) =>
                new ReplayGetDto(replay,
                    rpTuple.Select(tup => new GetRpJoinedPlayer(tup.rp, tup.p)))).ToListAsync();
    }

    public Task<Replay?> GetReplay(int id)
    {
        return this.Context.Replays.Include(r => r.ReplayPlayers).FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Tuple<int, string?>> UploadReplayNoEloDifference(ReplayPostDto input)
    {
        Replay replay = new Replay(input);
        
        this.Context.Replays.Add(replay);
        bool uploaded = await this.SaveChangesCheckConflict();

        return uploaded ? new Tuple<int, string?>(200, "Successfully uploaded replay")
                        : new Tuple<int, string?>(409, "Replay alerady exists");
    }

    private class RpJoinedPlayer
    {
        public ReplayPlayerPostDto rp { get; set; }
        public Player? p { get; set; }
    }

    public async Task<Tuple<int, UploadReplayResponse>> UploadReplay(ReplayPostDto input)
    {
        //gets the type of elo to add if the player's not registered yet
        var eloProp = GetEloProperty(input.ReplayPlayers.Count, input.Franchise);
        
        //groupJoin because the player might not exist
        IEnumerable<RpJoinedPlayer> joined = input.ReplayPlayers
            .GroupJoin(
                this.Context.Players,
                rp => rp.PlayerId,
                p => p.Id,
                (rp, players) => new { rp, p = players.FirstOrDefault() }
            )
            .Select(x => new RpJoinedPlayer { rp = x.rp, p = x.p });


        List<ReplayPlayerWithPlayer> rpJoinedPlayers = new(input.IsTeamGame ? 20 : 2);

        foreach (var row in joined)
        {
            if (row.p is null)
            {
                //player isn't in database
                row.p = new Player()
                {
                    Id = row.rp.PlayerId,
                    Nickname = row.rp.Nickname,
                    SdElo = null,
                    SdTeamGameElo = null,
                    WarnoElo = null,
                    WarnoTeamGameElo = null
                };
                eloProp.SetValue(row.p, 1200 + (row.rp.Elo - 1200) / 5);

                this.Context.Players.Add(row.p);
            }
            //he is but has no elo (has only different elo type) assign him one
            else if (eloProp.GetValue(row.p) is null)
            {
                eloProp.SetValue(row.p, 1200 + (row.rp.Elo - 1200) / 3);
            }

            //class with both RP and P (to avoid more DB queries when updating)
            rpJoinedPlayers.Add(new ReplayPlayerWithPlayer()
            {
                UploadReplayPlayerPost =
                    new UploadReplayPlayerResponse(row.rp, row.p.Nickname, row.p.DiscordId, (double)eloProp.GetValue(row.p)!),
                Player = row.p
            });
        }
        

        //if the replay has no skill level specified, find the channel it was uploaded in
        if (input.SkillLevel is null)
        {
            var channel = await this.Context.Channels.FindAsync(input.UploadedIn);
            input.SkillLevel = channel?.SkillLevel ?? SkillLevel.others;
        }
        
        //create replay model and upload it (replayPlayers are included in ctor)
        Replay replay = new(input);
        this.Context.Replays.Add(replay);
        
        int status = await SaveChangesCheckConflict() ? 200 : 409;
        
        // this.Context.ReplayPlayers.AddRange(rps);

        
        //for "others" SL ELO isn't updated
        if (input.SkillLevel == SkillLevel.others || status == 409)
        {
            List<UploadReplayPlayerResponse> uploadRp =
                rpJoinedPlayers.Select(rp => rp.UploadReplayPlayerPost).ToList();
            
            return new Tuple<int, UploadReplayResponse>(status, new UploadReplayResponse(replay, uploadRp));
        }
        
        //update Elo
        var playerService = new PlayersService(this.config);
        await playerService.UpdatePlayersElo(rpJoinedPlayers, input.Franchise);
        
        List<UploadReplayPlayerResponse> outputRp =
            rpJoinedPlayers.Select(rp => rp.UploadReplayPlayerPost).ToList();
        
        return new Tuple<int, UploadReplayResponse>(status, new UploadReplayResponse(replay, outputRp));
    }

    /// <summary>
    /// Saves changes in context, looks out for the duplicate exception (only that one)
    /// </summary>
    /// <returns>Returns true if replay was successfully uploaded, false if it is a duplicate</returns>
    private async Task<bool> SaveChangesCheckConflict()
    {
        try
        {
            await this.Context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            return false;
        }

        return true;
    }

    public async Task<Replay?> UploadReplayBansReport(ReplayBansReport report)
    {
       var replaysWithJoinedPlayers = await this.Context.Replays
            .Where(r => r.UploadedIn == report.ChannelId 
                        && r.UploadedAt <= DateTime.Now.AddDays(-7)
                        && r.IsTeamGame == false
                        && this.Context.DivisionBans.Any(db => db.ReplayId == r.Id))
           .GroupJoin(this.Context.ReplayPlayers
                .Join(this.Context.Players, rp => rp.PlayerId, p => p.Id, (rp, p) => new { rp, p }),
            r => r.Id, rp => rp.rp.PlayerId,
            (replay, rpWithPlayer) =>
                new ReplayGetDto(replay,
                    rpWithPlayer.Select(tup => new GetRpJoinedPlayer(tup.rp, tup.p)))).ToListAsync();
       

        for(int i = 0; i < report.Host.DivPicks.Count; i++)
        {
            string? map = report.Host.MapPicks.FirstOrDefault(m => m.Order == i)?.Pick;
            map ??= report.Guest.MapPicks.FirstOrDefault(m => m.Order == i)?.Pick;

            if (map is null)
            {
                throw new ArgumentException("Map pick for order " + i + " not found");
            }

            var target = replaysWithJoinedPlayers.Where(r =>
                (r.ReplayPlayers[0].DiscordId == report.Host.DiscordId
                 && r.ReplayPlayers[0].Division == report.Host.DivPicks[i].Pick
                 && r.ReplayPlayers[1].DiscordId == report.Guest.DiscordId
                 && r.ReplayPlayers[1].Division == report.Guest.DivPicks[i].Pick)
                ||

                (r.ReplayPlayers[1].DiscordId == report.Host.DiscordId
                 && r.ReplayPlayers[1].Division == report.Host.DivPicks[i].Pick
                 && r.ReplayPlayers[0].DiscordId == report.Guest.DiscordId
                 && r.ReplayPlayers[0].Division == report.Guest.DivPicks[i].Pick)
                && r.Map == map
            );

            int count = target.Count();
            
            if(count == 0)
            {
                return null;
            }
            
            ReplayGetDto replay = target.First();

            if (count > 1)
            {
                replay = target.OrderByDescending(r => r.UploadedAt).First();
            }
            
            Replay replay = 

        }
        
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