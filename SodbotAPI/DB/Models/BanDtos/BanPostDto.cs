namespace SodbotAPI.DB.Models.BanDtos;

public class Bans
{
    public string DiscordPlayerId { get; set; }
    public string[]? MapBans { get; set; }
    public int[]? DivisionBans { get; set; }
}