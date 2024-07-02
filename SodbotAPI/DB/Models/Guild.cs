using System.ComponentModel.DataAnnotations.Schema;

namespace SodbotAPI.DB.Models;

[Table("guilds")]
public class Guild
{
    [Column("id")]
    public string Id { get; set; }
    [Column("name")]
    public string Name { get; set; }
    [ForeignKey("GuildId")]
    public List<Channel> Channels { get; set; }
}