namespace SodbotAPI.DB.Models.ReplaysDtos.ReplayPlayers;

public class GetReplayPlayer
{
    public int PlayerId { get; set; }
    public string Nickname { get; set; }
    public double Elo { get; set; }
    public bool? MapSide { get; set; }
    public bool Victory { get; set; }
    public int Division { get; set; }
    public Income? Income { get; set; } //warno has no income
    public string DeckCode { get; set; }

    public GetReplayPlayer(ReplayPlayer rp)
    {
        this.PlayerId = rp.PlayerId;    
        this.Nickname = rp.Nickname;
        this.Elo = rp.Elo;
        this.MapSide = rp.MapSide;
        this.Victory = rp.Victory;
        this.Division = rp.Division;
        this.Income = rp.Income;
        this.DeckCode = rp.DeckCode;
    }
}