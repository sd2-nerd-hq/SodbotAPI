using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SodbotAPI.DB.Models;
[Table("division_bans")]
[PrimaryKey("PlayerId", "ReplayId", "DivisionId")]
public class DivisionBan
{
    [Column("id_player")]
    public int PlayerId { get; set; }
    [Column("id_replay")]
    public int ReplayId { get; set; }
    [Column("id_division")]
    public int DivisionId { get; set; }

    public DivisionBan(int playerId, int replayId, int divisionId)
    {
       this.PlayerId = playerId;
       this.ReplayId = replayId; 
       this.DivisionId = divisionId;
    }
}