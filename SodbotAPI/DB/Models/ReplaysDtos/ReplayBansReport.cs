namespace SodbotAPI.DB.Models.ReplaysDtos;

public class ReplayBansReport
{
    public ReplayBansReportPlayer Host { get; set; }
    public ReplayBansReportPlayer Guest { get; set; }
    public string ChannelId { get; set; }
    public bool PersistentSearch { get; set; }

    public ReplayBansReport(ReplayBansReportPlayer host, ReplayBansReportPlayer guest, string channelId, bool persistentSearch = false)
    {
       this.Host = host;
       this.Guest = guest;
       this.ChannelId = channelId;
       this.PersistentSearch = persistentSearch;
    }

    public ReplayBansReport()
    {
        
    }
}

public class ReplayBansReportPlayer
{
    public string DiscordId { get; set; }
    public List<string> DivBans { get; set; }
    public List<string> MapBans { get; set; }
    public List<DivPickWithOrder> DivPicks { get; set; }
    public List<MapPickWithOrder> MapPicks { get; set; }
}

public class DivPickWithOrder
{
    public int Order { get; set; }
    public int Pick { get; set; }
}
public class MapPickWithOrder
{
    public int Order { get; set; }
    public string Pick { get; set; }
}