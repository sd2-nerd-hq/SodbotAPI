namespace SodbotAPI.DB.Models.ReplaysDtos;

public class ReplayPostDto
{
        public string SessionId { get; set; }
        
        public string UploadedIn { get; set; }
        
        public string UploadedBy { get; set; }
        
        public DateTime UploadedAt { get; set; } = DateTime.Now;
        
        public Franchise Franchise { get; set; }
        
        public int Version { get; set; }

        public bool IsTeamGame { get; set; }
        
        public string Map { get; set; }
        
        public MapType? MapType { get; set; }
        
        public VictoryCondition VictoryCondition { get; set; }
        
        public int DurationSec { get; set; }
        
        public SkillLevel? SkillLevel { get; set; }
        
        public List<ReplayPlayerPostDto> ReplayPlayers { get; set; }
}