using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
namespace SodbotAPI.DB.Models;

[Table("players")]
public class Player
{
    [Column("id")]
    public int Id { get; set; }
    [Column("discord_id")]
    public string? DiscordId { get; set; }
    [Column("sd_elo")]
    public double? SdElo { get; set; }
    [Column("sd_team_game_elo")]
    public double? SdTeamGameElo { get; set; }
    [Column("warno_elo")]
    public double? WarnoElo { get; set; }
    [Column("warno_team_game_elo")]
    public double? WarnoTeamGameElo { get; set; }
    [Column("nickname")]
    public string Nickname { get; set; }
}