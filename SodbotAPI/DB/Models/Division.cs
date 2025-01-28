using System.ComponentModel.DataAnnotations.Schema;

namespace SodbotAPI.DB.Models;

[Table("divisions")]
public class Division
{
    [Column("id")]
    public int Id { get; set; }
    [Column("name")]
    public string Name { get; set; }
    [Column("franchise")]
    public Franchise Franchise { get; set; }
    [Column("faction")]
    public bool Faction { get; set; }
    [Column("nation")]
    public Nation Nation { get; set; }
    [Column("alias")]
    public string[] Alias { get; set; }
}

public enum Nation {
    germany,
    hungary,
    romania_ax,
    finland,
    italy_ax,
    italy_al,
    bulgaria,
    ussr,
    usa,
    uk,
    france,
    poland,
    canada,
    south_africa,
    romania_al,
    new_zealand,
    yugoslavia,
    czechoslovakia,
    germany_west,
    germany_east,
    belgium,
    netherlands,
    spain,
    sweden
}