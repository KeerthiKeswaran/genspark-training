using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Event.Contracts.IServices;

namespace Event.API.Controllers
{
    [ApiController]
    [Route("api/regions")]
    public class RegionController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public RegionController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRegions()
        {
            try
            {
                var regions = await _adminService.GetAllRegionsAsync();
                return Ok(regions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
