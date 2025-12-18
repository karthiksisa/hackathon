using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FDCS.CRM.Backend.Data;
using FDCS.CRM.Backend.DTOs;
using FDCS.CRM.Backend.Models;
using System.Security.Claims;

namespace FDCS.CRM.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LeadsController : ControllerBase
    {
        private readonly CrmDbContext _context;
        private readonly ILogger<LeadsController> _logger;

        public LeadsController(CrmDbContext context, ILogger<LeadsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all leads with optional filtering
        /// </summary>
        /// <summary>
        /// Get all leads with optional filtering
        /// </summary>

        [HttpGet]
        public async Task<ActionResult<List<LeadDTO>>> GetLeads(
            [FromQuery] string? status = null,
            [FromQuery] int? ownerId = null,
            [FromQuery] int? regionId = null,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null)
        {
            try
            {
                var query = _context.Leads
                    .Include(l => l.Owner)
                    .Include(l => l.Region) // Include Region
                    .AsQueryable();

                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId);

                // No default filtering based on role - return all leads
                // if (userRole == "Sales Rep") ... removed
                // if (userRole == "Regional Lead") ... removed
                // Super Admin: All leads (no default filter)

                // Filters
                if (!string.IsNullOrEmpty(status))
                    query = query.Where(l => l.Status == status);

                if (ownerId.HasValue)
                    query = query.Where(l => l.OwnerId == ownerId);

                if (regionId.HasValue)
                    query = query.Where(l => l.RegionId == regionId);

                if (dateFrom.HasValue)
                    query = query.Where(l => l.CreatedDate >= dateFrom);

                if (dateTo.HasValue)
                    query = query.Where(l => l.CreatedDate <= dateTo);

                var leads = await query.ToListAsync();
                var dtos = leads.Select(MapToDTO).ToList();

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting leads: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<LeadDTO>> GetLead(int id)
        {
            try
            {
                var lead = await _context.Leads
                    .Include(l => l.Owner)
                    .Include(l => l.Region)
                    .FirstOrDefaultAsync(l => l.Id == id);
                
                if (lead == null)
                    return NotFound();

                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId);

                if (userRole == "Sales Rep")
                {
                    if (lead.OwnerId != currentUserId) return Forbid();
                }
                else if (userRole == "Regional Lead")
                {
                     var currentUser = await _context.Users.Include(u => u.UserRegions).FirstOrDefaultAsync(u => u.Id == currentUserId);
                     if (currentUser != null && lead.RegionId.HasValue)
                     {
                         var regionIds = currentUser.UserRegions.Select(ur => ur.RegionId).ToList();
                         if (currentUser.RegionId.HasValue) regionIds.Add(currentUser.RegionId.Value);
                         if (!regionIds.Contains(lead.RegionId.Value)) return Forbid();
                     }
                     else
                     {
                         return Forbid();
                     }
                }

                return Ok(MapToDTO(lead));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting lead: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<LeadDTO>> CreateLead([FromBody] CreateLeadRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId);

                var lead = new Lead
                {
                    Name = request.Name,
                    Company = request.Company,
                    Email = request.Email,
                    Phone = request.Phone,
                    Source = request.Source,
                    Notes = request.Notes,
                    Status = "New",
                    CreatedDate = DateTime.Now
                };

                if (userRole == "Sales Rep")
                {
                    // Sales Rep: Auto-assign Owner and Region
                    lead.OwnerId = currentUserId;
                    var currentUser = await _context.Users.FindAsync(currentUserId);
                    if (currentUser?.RegionId != null)
                    {
                        lead.RegionId = currentUser.RegionId;
                    }
                    else
                    {
                         // Fallback or error if Sales Rep has no region? 
                         // For now, allow but they might be creating orphans if DB constraints don't catch it.
                    }
                }
                else if (userRole == "Regional Lead")
                {
                    var currentUser = await _context.Users.Include(u => u.UserRegions).FirstOrDefaultAsync(u => u.Id == currentUserId);
                    if (currentUser == null) return Unauthorized();

                    var allowedRegions = currentUser.UserRegions.Select(ur => ur.RegionId).ToList();
                    if (currentUser.RegionId.HasValue) allowedRegions.Add(currentUser.RegionId.Value);

                    // Region Validation
                    if (request.RegionId.HasValue)
                    {
                        if (!allowedRegions.Contains(request.RegionId.Value))
                            return BadRequest(new { message = "Cannot create lead in a region you are not assigned to." });
                        lead.RegionId = request.RegionId;
                    }
                    else
                    {
                        // Default to primary
                        if (currentUser.RegionId.HasValue) lead.RegionId = currentUser.RegionId.Value;
                        else return BadRequest(new { message = "RegionId is required." });
                    }

                    // Owner Assignment (Optional)
                    lead.OwnerId = request.OwnerId; // Can be null (Unassigned)
                }
                else // Admin
                {
                    lead.OwnerId = request.OwnerId;
                    lead.RegionId = request.RegionId;
                }

                _context.Leads.Add(lead);
                await _context.SaveChangesAsync();
                
                // Re-fetch to populate navigation props for DTO
                var createdLead = await _context.Leads
                    .Include(l => l.Owner)
                    .Include(l => l.Region)
                    .FirstOrDefaultAsync(l => l.Id == lead.Id);

                return CreatedAtAction(nameof(GetLead), new { id = lead.Id }, MapToDTO(createdLead));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating lead: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<object>> UpdateLead(int id, [FromBody] UpdateLeadRequest request)
        {
            try
            {
                var lead = await _context.Leads.FindAsync(id);
                if (lead == null)
                    return NotFound();

                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId);

                if (userRole == "Sales Rep")
                {
                    if (lead.OwnerId != currentUserId) return Forbid();
                }
                else if (userRole == "Regional Lead")
                {
                    // Validation could go here...
                }

                if (!string.IsNullOrEmpty(request.Name)) lead.Name = request.Name;
                if (!string.IsNullOrEmpty(request.Status)) lead.Status = request.Status;
                if (!string.IsNullOrEmpty(request.Company)) lead.Company = request.Company;
                if (!string.IsNullOrEmpty(request.Email)) lead.Email = request.Email;
                if (!string.IsNullOrEmpty(request.Phone)) lead.Phone = request.Phone;
                if (!string.IsNullOrEmpty(request.Notes)) lead.Notes = request.Notes;

                if (request.RegionId.HasValue) lead.RegionId = request.RegionId;
                if (request.OwnerId.HasValue) lead.OwnerId = request.OwnerId; // Allow assignment/reassignment

                lead.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Lead updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating lead: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        [HttpPost("{id}/convert")]
        [Authorize(Roles = "Super Admin")]
        public async Task<ActionResult<object>> ConvertLead(int id, [FromBody] ConvertLeadRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var lead = await _context.Leads.FindAsync(id);
                if (lead == null) return NotFound();
                
                var account = new Account
                {
                    Name = request.AccountName,
                    RegionId = request.RegionId,
                    SalesRepId = request.SalesRepId,
                    Status = "Prospect",
                    CreatedDate = DateTime.Now
                };

                lead.Status = "Converted";
                lead.ConvertedAt = DateTime.UtcNow;

                _context.Accounts.Add(account);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Lead converted successfully", accountId = account.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error converting lead: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Super Admin")]
        public async Task<ActionResult<object>> DeleteLead(int id)
        {
             try
            {
                var lead = await _context.Leads.FindAsync(id);
                if (lead == null) return NotFound();
                _context.Leads.Remove(lead);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Lead deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting lead: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        private LeadDTO MapToDTO(Lead lead)
        {
            return new LeadDTO
            {
                Id = lead.Id,
                Name = lead.Name,
                Company = lead.Company,
                Email = lead.Email,
                Phone = lead.Phone,
                Status = lead.Status,
                OwnerId = lead.OwnerId,
                Source = lead.Source,
                CreatedDate = lead.CreatedDate,
                ConvertedAt = lead.ConvertedAt,
                ownerName = lead.Owner?.Name,
                regionName = lead.Region?.Name ?? lead.Owner?.Region?.Name, 
                RegionId = lead.RegionId 
            };
        }
    }
}
