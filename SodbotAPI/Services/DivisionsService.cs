using Microsoft.EntityFrameworkCore;
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
    public async Task<List<Division>> UpdateDivisions(List<Division> divisions)
    {
        var existingDivisions = await this.Context.Divisions.ToListAsync();
        var updatedDivs = divisions.Where(d => !existingDivisions.Any(ed => ed.Id == d.Id)!).ToList();
        this.Context.Divisions.AddRange(updatedDivs);
        await this.Context.SaveChangesAsync();

        existingDivisions = await this.Context.Divisions.ToListAsync();
        
        foreach (var div in existingDivisions)
        {
            var uploaded = divisions.First(existing => div.Id == existing.Id);

            if (div.Name != uploaded.Name
               || div.Nation != uploaded.Nation
               || div.Franchise != uploaded.Franchise
               || div.Faction != uploaded.Faction)
            {
                var toUpdate = await this.Context.Divisions.FirstAsync(d => d.Id == div.Id);
                
                toUpdate.Name = uploaded.Name;
                toUpdate.Nation = uploaded.Nation;
                toUpdate.Franchise = uploaded.Franchise;
                toUpdate.Faction = uploaded.Faction;
                
                this.Context.Divisions.Update(toUpdate);
                
                await this.Context.SaveChangesAsync();
                
                updatedDivs.Add(div);
            }
        } 
        
        
        
        return updatedDivs;
    }
}