using SodbotAPI.DB.Models.ReplaysDtos.ReplayPlayers;

namespace SodbotAPI.DB.Models.ReplaysDtos;

public class ReplayGetDto(Replay r, IEnumerable<GetRpJoinedPlayer> joinedPlayers)
{
    public int Id { get; set; } = r.Id;
    public string SessionId { get; set; } = r.SessionId;
    public string UploadedIn { get; set; } = r.UploadedIn;
    public string UploadedBy { get; set; } = r.UploadedBy;
    public DateTime UploadedAt { get; set; } = r.UploadedAt;
    public Franchise Franchise { get; set; } = r.Franchise;
    public int Version { get; set; } = r.Version;
    public bool IsTeamGame { get; set; } = r.IsTeamGame;
    public string Map { get; set; } = r.Map;
    public MapType? MapType { get; set; } = r.MapType;
    public VictoryCondition VictoryCondition { get; set; } = r.VictoryCondition;
    public int DurationSec { get; set; } = r.DurationSec;
    public SkillLevel SkillLevel { get; set; } = r.SkillLevel;
    public IEnumerable<GetRpJoinedPlayer> ReplayPlayers { get; set; } = joinedPlayers;
}