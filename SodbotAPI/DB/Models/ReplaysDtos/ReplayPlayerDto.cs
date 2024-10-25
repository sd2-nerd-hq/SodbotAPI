namespace SodbotAPI.DB.Models.ReplaysDtos;

public class ReplayPlayerDto
{
    public ReplayPlayerDto()
    {
        
    }
    public ReplayPlayerDto(ReplayPlayer rp)
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
    
    public int PlayerId { get; set; }
    
    public string Nickname { get; set; }
    
    public double Elo { get; set; }

    public bool? MapSide { get; set; }

    public bool Victory { get; set; }

    public int Division { get; set; }

    public Income? Income { get; set; }
    
    public string DeckCode { get; set; }
}
