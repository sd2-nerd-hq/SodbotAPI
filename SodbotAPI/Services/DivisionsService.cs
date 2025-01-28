using Npgsql;
using SodbotAPI.DB;
using SodbotAPI.DB.Models;

namespace SodbotAPI.Services;

public class DivisionsService : SodbotService
{
    public DivisionsService(IConfiguration config)
    {
        this.Context = new AppDbContext(config);
    }
    
    public Division? GetDivision(string id)
    {
        return this.Context.Divisions.Find(id);
    }
    public List<Division> GetDivisions()
    {
        return this.Context.Divisions.ToList();
    }
    public List<Division> AddMissingDivisions(List<Division> divisions)
    {
        var existingDivisions = this.Context.Divisions.ToList();
        var missingDivisions = divisions.Where(d => !existingDivisions.Any(ed => ed.Id == d.Id)!).ToList();
        this.Context.Divisions.AddRange(missingDivisions);
        this.Context.SaveChanges();
        
        return missingDivisions;
    }
}