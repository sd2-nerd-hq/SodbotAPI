namespace SodbotAPI.DB.Models.GuildsDtos;

public class GuildPostDto
{
    public long Id { get; set; }
    public string Name { get; set; }
    public ChannelGetPostDto Channel { get; set; }
}