namespace SodbotAPI.DB.Models.ReplaysDtos;

public class ReplayDto
{
        public string SessionId { get; set; }
        
        public long UploadedIn { get; set; }
        
        public long UploadedBy { get; set; }
        
        public DateTime UploadedAt { get; set; } = DateTime.Now;
        
        public Franchise Franchise { get; set; }
        
        public int Version { get; set; }

        public bool IsTeamGame { get; set; }
        
        public string Map { get; set; }
        
        public MapType? MapType { get; set; }
        
        public VictoryCondition VictoryCondition { get; set; }
        
        public int DurationSec { get; set; }
        
        public SkillLevel? ReplayType { get; set; }
        
        public List<ReplayPlayerDto> ReplayPlayers { get; set; }
}