using Microsoft.EntityFrameworkCore.ChangeTracking;
using SodbotAPI.DB.Models.ReplaysDtos;
using SodbotAPI.DB.Models.ReplaysDtos.ReplayPlayers;

public class UploadReplayPlayerResponse : ReplayPlayerPostDto
{
    public string MostUsedNickname { get; set; }
    public string? DiscordId { get; set; }
    public double SodbotElo { get; set; }
    public double? OldSodbotElo { get; set; }

    public UploadReplayPlayerResponse()
    {
            
    }
    public UploadReplayPlayerResponse(ReplayPlayerPostDto rp, string mostUsedNickname, string? discordId, double sodbotElo, double? oldElo = null)
    {
        this.PlayerId = rp.PlayerId;
        this.Nickname = rp.Nickname;
        this.Elo = rp.Elo;
        this.MapSide = rp.MapSide;
        this.Victory = rp.Victory;
        this.Division = rp.Division;
        this.Income = rp.Income;
        this.DeckCode = rp.DeckCode;
        this.MostUsedNickname = mostUsedNickname;
        this.DiscordId = discordId;
        this.SodbotElo = sodbotElo;
        this.OldSodbotElo = oldElo;
    }
}