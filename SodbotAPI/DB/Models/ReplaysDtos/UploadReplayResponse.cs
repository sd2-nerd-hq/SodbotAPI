namespace SodbotAPI.DB.Models.ReplaysDtos;

public class UploadReplayResponse
{
    public int Id { get; set; }
    public string SessionId { get; set; }
    public string UploadedIn { get; set; }
    public string UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; }
    public Franchise Franchise { get; set; }
    public int Version { get; set; }
    public bool IsTeamGame { get; set; }
    public string Map { get; set; }
    public MapType? MapType { get; set; }
    public VictoryCondition VictoryCondition { get; set; }
    public int DurationSec { get; set; }
    public SkillLevel SkillLevel { get; set; }
    public List<UploadReplayPlayerResponse> ReplayPlayers { get; set; }

    public UploadReplayResponse()
    {
    }

    public UploadReplayResponse(Replay r, List<UploadReplayPlayerResponse> replayPlayers)
    {
        this.Id = r.Id;
        this.SessionId = r.SessionId;
        this.UploadedIn = r.UploadedIn;
        this.UploadedBy = r.UploadedBy;
        this.UploadedAt = r.UploadedAt;
        this.Franchise = r.Franchise;
        this.Version = r.Version;
        this.IsTeamGame = r.IsTeamGame;
        this.Map = r.Map;
        this.MapType = r.MapType;
        this.VictoryCondition = r.VictoryCondition;
        this.DurationSec = r.DurationSec;
        this.SkillLevel = r.SkillLevel;
        this.ReplayPlayers = replayPlayers;
    }
}