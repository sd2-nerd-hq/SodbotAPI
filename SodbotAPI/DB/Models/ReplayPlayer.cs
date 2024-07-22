using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SodbotAPI.DB.Models;


[Table("replay_players")]
[PrimaryKey("PlayerId", "ReplayId")]
public class ReplayPlayer
{
    [Column("id_player")]
    public int PlayerId { get; set; }
    
    [Column("id_replay")]
    public int ReplayId { get; set; }

    [Column("nickname")]
    public string Nickname { get; set; }
    
    [Column("elo")]
    public double Elo { get; set; }

    [Column("map_side")]
    public bool? MapSide { get; set; }

    [Column("victory")]
    public bool Victory { get; set; }

    [Column("division")]
    public int Division { get; set; }

    [Column("income")]
    public Income? Income { get; set; } //warno has no income
    
    [Column("deck_code")]
    public string DeckCode { get; set; }
}

public enum Income
{
    balanced,
    vanguard,
    maverick,
    juggernaut,
    flatline,
    vforvictory
}