using Microsoft.AspNetCore.Mvc;
using SodbotAPI.DB.Models;
using SodbotAPI.Services;

namespace SodbotAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class DivisionsController : Controller
{
    private IConfiguration config;
    public DivisionsController(IConfiguration config)
    {
        this.config = config;
    }
    
    [HttpGet]
    public IActionResult Get()
    {
        var service = new DivisionsService(this.config);

        var output = service.GetDivisions();
        
        return Ok(output);
    }
    
    [HttpPost]
    public IActionResult Post([FromBody] List<Division> input)
    {
        var service = new DivisionsService(this.config);

        var output = service.AddMissingDivisions(input);

        return Ok(output);
    }
}