namespace SodbotAPI.DB.Models.PlayersDtos;

public class PlayerWithRank
{
    public int Id { get; set; }
    public string? DiscordId { get; set; }
    public string Name { get; set; }
    public double Elo { get; set; }
    public int Rank { get; set; }
}