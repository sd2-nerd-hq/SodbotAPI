using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using SodbotAPI.DB.Models.ReplaysDtos;

namespace SodbotAPI.DB.Models;

[Table("replays")]
[PrimaryKey("Id")]
public class Replay
{
    [Column("id")] [BindNever] public int Id { get; set; }

    [Column("session_id")]
    [StringLength(36)]
    public string SessionId { get; set; }

    [Column("uploaded_in")] public string UploadedIn { get; set; }

    [Column("uploaded_by")] public string UploadedBy { get; set; }

    [Column("uploaded_at")] public DateTime UploadedAt { get; set; }
    [Column("franchise")] public Franchise Franchise { get; set; }

    [Column("version")] public int Version { get; set; }
    [Column("is_team_game")] public bool IsTeamGame { get; set; }
    [Column("map")] public string Map { get; set; }

    [Column("map_type")] public MapType? MapType { get; set; }

    [Column("victory_condition")] public VictoryCondition VictoryCondition { get; set; }

    [Column("duration_sec")] public int DurationSec { get; set; }

    [Column("skill_level")] public SkillLevel SkillLevel { get; set; }

    [ForeignKey("ReplayId")] public List<ReplayPlayer> ReplayPlayers { get; set; }

    public Replay()
    {
    }

    public Replay(ReplayPostDto dto)
    {
        this.SessionId = dto.SessionId;
        this.UploadedIn = dto.UploadedIn;
        this.UploadedBy = dto.UploadedBy;
        this.UploadedAt = dto.UploadedAt;
        this.Franchise = dto.Franchise;
        this.Version = dto.Version;
        this.IsTeamGame = dto.IsTeamGame;
        this.Map = dto.Map;
        this.MapType = dto.MapType;
        this.VictoryCondition = dto.VictoryCondition;
        this.DurationSec = dto.DurationSec;
        this.SkillLevel = dto.SkillLevel ?? SkillLevel.others;
        
        this.ReplayPlayers = dto.ReplayPlayers.Select(r => new ReplayPlayer()
        {
            PlayerId = r.PlayerId,
            Nickname = r.Nickname,
            Elo = r.Elo,
            MapSide = r.MapSide,
            Victory = r.Victory,
            Division = r.Division,
            Income = r.Income,
            DeckCode = r.DeckCode
        }).ToList();
    }
}

public enum SkillLevel
{
    others,
    div1,
    div2,
    div3,
    div4,
    div5
}

public enum MapType
{
    _1v1,
    _2v2,
    _3v3,
    _4v4,
    _10v10
}

public enum VictoryCondition
{
    draw,
    minor,
    major,
    total
}