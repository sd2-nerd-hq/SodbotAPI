namespace SodbotAPI.DB.Models.ReplaysDtos.ReplayPlayers;


//For Replays.GetReplaysWithPlayers()
//Combines information from the replay with information from the players table
public class GetRpJoinedPlayer(ReplayPlayer rp, Player p) : GetReplayPlayer(rp)
{
    public int ReplayId { get; set; } = rp.PlayerId;
    public string? DiscordId { get; set; } = p.DiscordId;
    public double? SdElo { get; set; } = p.SdElo;
    public double? SdTeamGameElo { get; set; } = p.SdTeamGameElo;
    public double? WarnoElo { get; set; } = p.WarnoElo;
    public double? WarnoTeamGameElo { get; set; } = p.WarnoTeamGameElo;
    public string MostUsedNickname { get; set; } = p.Nickname;
}



