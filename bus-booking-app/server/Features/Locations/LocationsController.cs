using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Infrastructure.Data;

namespace server.Features.Locations
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LocationsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("cities")]
        public async Task<IActionResult> GetCities()
        {
            var cities = await _context.Cities
                .Select(c => new { c.Id, c.Name, c.State })
                .OrderBy(c => c.Name)
                .ToListAsync();

            return Ok(cities);
        }

        [HttpGet("hubs")]
        public async Task<IActionResult> GetHubs([FromQuery] Guid cityId)
        {
            var hubs = await _context.Hubs
                .Where(h => h.CityId == cityId)
                .Select(h => new { h.Id, h.Name, Type = h.Type.ToString() })
                .OrderBy(h => h.Name)
                .ToListAsync();

            return Ok(hubs);
        }
    }
}
