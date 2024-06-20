namespace SodbotAPI.DB.Models.GuildsDtos;

public class ChannelGetPostDto
{
    public long Id { get; set; }
    
    public string Name { get; set; }

    public SkillLevel SkillLevel { get; set; } = SkillLevel.others;

    public Franchise PrimaryMode { get; set; } = Franchise.sd2;
}