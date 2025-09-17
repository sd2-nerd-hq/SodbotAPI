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

        var ret = this.Context.Replays.Add(replay);
        
        int status = await SaveChangesCheckConflict() ? 0 : 1;
        
        //when uploaded again in a different tournament channel resubmit the replay (was probably a mistake)
        if (input.SkillLevel != SkillLevel.others)
        {
            
            //if it got uploaded in a tournament channel before don't alter elo anymore (to avoid multiple ELO changes)
            if (status == 1 && ret.Entity.SkillLevel == SkillLevel.others)
            {
                
            }
        }
        
        //for "others" SL ELO isn't updated
        if (input.SkillLevel == SkillLevel.others || status == 1)
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

    //used to upload finished pick asd bans session done on aoe2 website
    //to make things simpler and avoid remembering a message ID in replay database
    //it guesses the replay based on the pick data (divs and map) and the players in the game
    public async Task<Tuple<int, List<Replay>?>> UploadReplayBansReport(ReplayBansReport report)
    {

        var hostTask= this.Context.Players.FirstOrDefaultAsync(p => p.DiscordId == report.Host.DiscordId);
        var guestTask= this.Context.Players.FirstOrDefaultAsync(p => p.DiscordId == report.Guest.DiscordId);
        
      
       var queryTask = this.Context.Replays
           .Include(r => r.ReplayPlayers)
            .Where(r => r.UploadedIn == report.ChannelId 
                        && r.UploadedAt <= DateTime.Now.AddDays(-7)
                        && r.IsTeamGame == false
                        && this.Context.DivisionBans.Any(db => db.ReplayId == r.Id))
           .ToListAsync(); 
       
       var host = await hostTask;
       var guest = await guestTask;

       if (host is null)
       {
           return new (1, null);
       }

       if (guest is null)
       {
           return new(2, null);
       }

       //create models that will be uploaded to the DB
       List<DivisionBan> divBans = new();
       List<MapBan> mapBans = new();
       
       report.Host.DivBans.ForEach(db => divBans.Add(new DivisionBan(host.Id, 0, db)));
       report.Guest.DivBans.ForEach(db => divBans.Add(new DivisionBan(guest.Id, 0, db)));
       
       report.Host.MapBans.ForEach(mb => mapBans.Add(new MapBan(host.Id, 0, mb)));
       report.Guest.MapBans.ForEach(mb => mapBans.Add(new MapBan(guest.Id, 0, mb)));
       
       
       var replays = await queryTask;
       List<Replay> results = new();
       
        //iterates through the picks
        for(int i = 0; i < report.Host.DivPicks.Count; i++)
        {
            string? map = report.Host.MapPicks.FirstOrDefault(m => m.Order == i)?.Pick;
            map ??= report.Guest.MapPicks.FirstOrDefault(m => m.Order == i)?.Pick;

            if (map is null)
            {
                throw new ArgumentException("Map pick for order " + i + " not found");
            }

            //finds the replays corresponding to the reports
            var target = replays.Where(r =>
                r.Map == map &&
                (r.ReplayPlayers[0].PlayerId == host.Id
                 && r.ReplayPlayers[0].Division == report.Host.DivPicks[i].Pick
                 && r.ReplayPlayers[1].PlayerId == guest.Id
                 && r.ReplayPlayers[1].Division == report.Guest.DivPicks[i].Pick)
                ||
                (r.ReplayPlayers[1].PlayerId == host.Id 
                 && r.ReplayPlayers[1].Division == report.Host.DivPicks[i].Pick
                 && r.ReplayPlayers[0].PlayerId == guest.Id
                 && r.ReplayPlayers[0].Division == report.Guest.DivPicks[i].Pick)
            ).ToList();
            
            if(target.Count == 0)
            {
                return new(3, null);
            }
            
            Replay replay = target.First();

            //if it finds more than one select the newest one
            //possibly sketchy? if persistant search is used (I guess the user knows why he's doing it... (trusting the user, what could go wrong?)
            //maybe check if the other one was played somewhat close to this one then?
            if (target.Count > 1)
            {
                replay = target.OrderByDescending(r => r.UploadedAt).First();
            }
            
            divBans.ForEach(db => db.ReplayId = replay.Id);
            mapBans.ForEach(mb => mb.ReplayId = replay.Id);
            
            //now add the picks and bans to db 
            this.Context.DivisionBans.AddRange(divBans);
            this.Context.MapBans.AddRange(mapBans);
            
            results.Add(replay);
        }
        
        await this.Context.SaveChangesAsync();
        
        return new (0, results);
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