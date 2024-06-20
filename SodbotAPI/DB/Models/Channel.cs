using System.ComponentModel.DataAnnotations.Schema;

namespace SodbotAPI.DB.Models;

[Table("channels")]
public class Channel
{
    [Column("id")]
    public long Id { get; set; }
    [Column("id_guild")]
    public long GuildId { get; set; }
    [Column("name")]
    public string Name { get; set; }
    [Column("skill_level")]
    public SkillLevel SkillLevel { get; set; }
    [Column("primary_mode")] 
    public Franchise PrimaryMode { get; set; }
}

public enum Franchise
{
    sd2,
    warno
}