using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SodbotAPI.DB.Models;
[Table("map_bans")]
[PrimaryKey("PlayerId", "ReplayId", "Map")]
public class MapBan
{
    [Column("id_player")]
    public int PlayerId { get; set; }
    [Column("id_replay")]
    public int ReplayId { get; set; }
    [Column("map")]
    public string Map { get; set; }

    public MapBan(int playerId, int replayId, string map)
    {
       this.PlayerId = playerId;
       this.ReplayId = replayId;
       this.Map = map;
    }
}
