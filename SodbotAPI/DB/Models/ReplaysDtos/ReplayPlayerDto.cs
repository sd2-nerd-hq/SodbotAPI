namespace SodbotAPI.DB.Models.ReplaysDtos;

public class ReplayPlayerDto
{
    public int PlayerId { get; set; }
    
    public string Nickname { get; set; }
    
    public double Elo { get; set; }

    public bool? MapSide { get; set; }

    public bool Victory { get; set; }

    public int Division { get; set; }

    public Income? Income { get; set; }
    
    public string DeckCode { get; set; }
}