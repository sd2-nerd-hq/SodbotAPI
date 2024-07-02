namespace SodbotAPI.DB.Models.GuildsDtos;

public class GuildGetDto
{
    public string Id { get; set; }
    
    public string Name { get; set; }
    
    public List<ChannelGetPostDto> Channels { get; set; }
}