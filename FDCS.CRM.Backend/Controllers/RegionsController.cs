using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FDCS.CRM.Backend.Data;
using FDCS.CRM.Backend.DTOs;
using FDCS.CRM.Backend.Models;

namespace FDCS.CRM.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RegionsController : ControllerBase
    {
        private readonly CrmDbContext _context;
        private readonly ILogger<RegionsController> _logger;

        public RegionsController(CrmDbContext context, ILogger<RegionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all regions
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<RegionDTO>>> GetRegions()
        {
            try
            {
                var regions = await _context.Regions.ToListAsync();
                var dtos = regions.Select(r => new RegionDTO
                {
                    Id = r.Id,
                    Name = r.Name,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                }).ToList();

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting regions: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Get region by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<RegionDTO>> GetRegion(int id)
        {
            try
            {
                var region = await _context.Regions.FindAsync(id);
                if (region == null)
                    return NotFound();

                var dto = new RegionDTO
                {
                    Id = region.Id,
                    Name = region.Name,
                    CreatedAt = region.CreatedAt,
                    UpdatedAt = region.UpdatedAt
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting region: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Create new region
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Super Admin")]
        public async Task<ActionResult<RegionDTO>> CreateRegion([FromBody] CreateRegionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var region = new Region { Name = request.Name };
                _context.Regions.Add(region);
                await _context.SaveChangesAsync();

                var dto = new RegionDTO
                {
                    Id = region.Id,
                    Name = region.Name,
                    CreatedAt = region.CreatedAt,
                    UpdatedAt = region.UpdatedAt
                };

                return CreatedAtAction(nameof(GetRegion), new { id = region.Id }, dto);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating region: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Update region
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Super Admin")]
        public async Task<ActionResult<object>> UpdateRegion(int id, [FromBody] UpdateRegionRequest request)
        {
            try
            {
                var region = await _context.Regions.FindAsync(id);
                if (region == null)
                    return NotFound();

                region.Name = request.Name;
                region.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                return Ok(new { message = "Region updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating region: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Delete region
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Super Admin")]
        public async Task<ActionResult<object>> DeleteRegion(int id)
        {
            try
            {
                var region = await _context.Regions.FindAsync(id);
                if (region == null)
                    return NotFound();

                _context.Regions.Remove(region);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Region deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting region: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }
    }
}
