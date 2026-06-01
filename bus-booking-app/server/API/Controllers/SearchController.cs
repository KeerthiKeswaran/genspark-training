using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Infrastructure.Data;
using Microsoft.Extensions.Caching.Memory;

namespace server.Features.Search
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SearchController> _logger;
        private readonly IMemoryCache _cache;

        public SearchController(AppDbContext context, ILogger<SearchController> logger, IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        [HttpGet]
        public async Task<IActionResult> SearchBuses([FromQuery] string from, [FromQuery] string to, [FromQuery] DateTime date)
        {
            _logger.LogInformation("--- SEARCH VERSION 2.1 ---");
            _logger.LogInformation("Searching buses: {From} to {To} on {Date}", from, to, date);

            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
                return BadRequest("Source and Destination are required.");

            var fromTerm = from.Trim();
            var toTerm = to.Trim();
            
            var startDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
            var endDate = startDate.AddDays(1);

            var now = DateTime.UtcNow;
            var minDeparture = now.AddMinutes(10);

            var results = await _context.Schedules
                .Include(s => s.Bus)
                .ThenInclude(b => b!.Operator)
                .Include(s => s.Route)
                .Where(s => EF.Functions.ILike(s.Route!.Source, fromTerm) && 
                            EF.Functions.ILike(s.Route!.Destination, toTerm) &&
                            s.DepartureTime >= startDate && 
                            s.DepartureTime < endDate &&
                            s.DepartureTime >= minDeparture &&
                            s.Status == server.Core.Enums.JourneyStatus.Scheduled)
                .Select(s => new BusSearchResult(
                    s.Id,
                    s.Bus!.BusNumber,
                    s.Bus.BusType,
                    s.Bus.Operator!.CompanyName,
                    s.Bus.Operator.Address,
                    s.Route!.Source,
                    s.Route.Destination,
                    s.DepartureTime,
                    s.ArrivalTime,
                    s.Price,
                    s.AvailableSeats
                ))
                .ToListAsync();

            _logger.LogInformation("Found {Count} results", results.Count);

            return Ok(results);
        }

        [HttpGet("cities")]
        public async Task<IActionResult> GetCities([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 1) return Ok(new List<string>());
            
            var searchTerm = query.Trim().ToLower();
            
            if (!_cache.TryGetValue("AllCities", out List<string>? allCities))
            {
                // Fetch sources and destinations separately to ensure EF Core translation
                var sources = await _context.Routes.Select(r => r.Source).Distinct().ToListAsync();
                var destinations = await _context.Routes.Select(r => r.Destination).Distinct().ToListAsync();
                
                allCities = sources.Union(destinations).Distinct().OrderBy(c => c).ToList();
                
                _cache.Set("AllCities", allCities, TimeSpan.FromMinutes(30));
            }

            var results = allCities!
                .Where(c => c.ToLower().Contains(searchTerm))
                .Take(10)
                .ToList();

            return Ok(results);
        }
    }
}
