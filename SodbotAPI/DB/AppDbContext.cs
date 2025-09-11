using Microsoft.EntityFrameworkCore;
using SodbotAPI.DB.Models;
using SodbotAPI.DB.Models.ReplaysDtos;

namespace SodbotAPI.DB;

public class AppDbContext : DbContext
{
    private readonly IConfiguration config;
    public AppDbContext(IConfiguration config)
    {
        this.config = config;
    }
    public DbSet<Replay> Replays { get; set; }
    public DbSet<Player> Players { get; set; }
    public DbSet<ReplayPlayer> ReplayPlayers { get; set; }
    public DbSet<DivisionBan> DivisionBans { get; set; }
    public DbSet<MapBan> MapBans { get; set; }    
    public DbSet<Division> Divisions { get; set; }
    public DbSet<Guild> Guilds { get; set; }
    public DbSet<Channel> Channels { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        base.OnConfiguring(options);
        
        options.UseNpgsql(this.config.GetConnectionString("ApiDatabase"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.HasPostgresEnum<Income>();


        modelBuilder.Entity<ReplayPlayer>(e =>
        {
            e.Property(p => p.Income).HasColumnType("Income");
        });

        modelBuilder.Entity<ReplayPlayer>()
            .HasMany(rp => rp.DivisionBans)
            .WithOne()
            .HasForeignKey(divBan => new { divBan.PlayerId, divBan.ReplayId });
        
        modelBuilder.Entity<ReplayPlayer>()
            .HasMany(rp => rp.MapBans)
            .WithOne()
            .HasForeignKey(mapBan => new { mapBan.PlayerId, mapBan.ReplayId });
        
        
        
        modelBuilder.Entity<Replay>(e =>
        {
            e.Property(p => p.SkillLevel).HasColumnType("SkillLevel");
            e.Property(p => p.MapType).HasColumnType("MapType");
            e.Property(p => p.VictoryCondition).HasColumnType("VictoryCondition");
        });
        
        modelBuilder.Entity<Channel>(e =>
        {
            e.Property(p => p.PrimaryMode).HasColumnType("Franchise");
            e.Property(p => p.SkillLevel).HasColumnType("SkillLevel");
        });
        
        modelBuilder.Entity<Division>(e =>
        {
            e.Property(p => p.Franchise).HasColumnType("Franchise");
            e.Property(p => p.Nation).HasColumnType("Nation");
        });
        
        
    }
}