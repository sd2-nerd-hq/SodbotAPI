
using Npgsql;
using SodbotAPI.DB.Models;

namespace SodbotAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            NpgsqlConnection.GlobalTypeMapper.MapEnum<SkillLevel>("skillLevel");
            NpgsqlConnection.GlobalTypeMapper.MapEnum<VictoryCondition>("victoryCondition");
            NpgsqlConnection.GlobalTypeMapper.MapEnum<MapType>("mapType");
            NpgsqlConnection.GlobalTypeMapper.MapEnum<Income>("income");
            NpgsqlConnection.GlobalTypeMapper.MapEnum<Franchise>("franchise");
            NpgsqlConnection.GlobalTypeMapper.MapEnum<Nation>("nation");

            // NpgsqlConnection.GlobalTypeMapper.EnableUnmappedTypes();
            
            var builder = WebApplication.CreateBuilder(args);
            
            // Add services to the container.

            builder.Services.AddControllers();
            
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
