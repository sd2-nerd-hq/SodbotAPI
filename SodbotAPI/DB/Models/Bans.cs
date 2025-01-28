using System.ComponentModel.DataAnnotations.Schema;

namespace SodbotAPI.DB.Models;

[Table("bans")]
public class Bans
{
    [Column("discord_id_player")]
    public string DiscordPlayerId { get; set; }
    [Column("map_bans")]
    public string[] MapBans { get; set; }
    [Column("division_bans")]
    public int[] DivisionBans { get; set; }
}