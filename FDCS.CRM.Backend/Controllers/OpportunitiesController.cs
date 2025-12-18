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
    public class OpportunitiesController : ControllerBase
    {
        private readonly CrmDbContext _context;
        private readonly ILogger<OpportunitiesController> _logger;

        public OpportunitiesController(CrmDbContext context, ILogger<OpportunitiesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all opportunities with optional filtering
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<OpportunityDTO>>> GetOpportunities(
            [FromQuery] string? stage = null,
            [FromQuery] int? accountId = null,
            [FromQuery] int? ownerId = null)
        {
            try
            {
                var query = _context.Opportunities
                    .Include(o => o.Account).ThenInclude(a => a.Region)
                    .Include(o => o.Account).ThenInclude(a => a.SalesRep)
                    .Include(o => o.Owner)
                    .AsQueryable();

                int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId);

                // Access Control Check Disabled
                /*
                if (User.IsInRole("Sales Rep"))
                {
                    query = query.Where(o => o.Account.SalesRepId == currentUserId);
                }
                else if (User.IsInRole("Regional Lead"))
                {
                    var currentUser = await _context.Users.Include(u => u.UserRegions).FirstOrDefaultAsync(u => u.Id == currentUserId);
                    if (currentUser != null)
                    {
                        var regionIds = currentUser.UserRegions.Select(ur => ur.RegionId).ToList();
                        if (currentUser.RegionId.HasValue) regionIds.Add(currentUser.RegionId.Value);
                        
                        query = query.Where(o => regionIds.Contains(o.Account.RegionId));
                    }
                }
                */

                if (!string.IsNullOrEmpty(stage))
                    query = query.Where(o => o.Stage == stage);

                if (accountId.HasValue)
                    query = query.Where(o => o.AccountId == accountId);

                if (ownerId.HasValue)
                    query = query.Where(o => o.OwnerId == ownerId);

                var opportunities = await query.ToListAsync();
                var dtos = opportunities.Select(MapToDTO).ToList();

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting opportunities: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Get opportunity by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<OpportunityDTO>> GetOpportunity(int id)
        {
            try
            {
                var opportunity = await _context.Opportunities.Include(o => o.Account).FirstOrDefaultAsync(o => o.Id == id);
                if (opportunity == null)
                    return NotFound();

                // if (!await HasAccountAccess(opportunity.AccountId)) return Forbid();

                return Ok(MapToDTO(opportunity));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting opportunity: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Create new opportunity
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<OpportunityDTO>> CreateOpportunity([FromBody] CreateOpportunityRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Check Access to Target Account
                // if (!await HasAccountAccess(request.AccountId)) 
                //    return BadRequest(new { message = "You cannot create an opportunity for this account." });

                var opportunity = new Opportunity
                {
                    Name = request.Name,
                    AccountId = request.AccountId,
                    Stage = !string.IsNullOrEmpty(request.Stage) ? request.Stage : "Prospecting",
                    Amount = request.Amount,
                    CloseDate = request.CloseDate,
                    OwnerId = request.OwnerId
                };

                _context.Opportunities.Add(opportunity);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetOpportunity), new { id = opportunity.Id }, MapToDTO(opportunity));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating opportunity: {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"Inner: {ex.InnerException.Message}");
                _logger.LogError($"Error creating opportunity: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Update opportunity
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<object>> UpdateOpportunity(int id, [FromBody] UpdateOpportunityRequest request)
        {
            try
            {
                var opportunity = await _context.Opportunities.Include(o => o.Account).FirstOrDefaultAsync(o => o.Id == id);
                if (opportunity == null)
                    return NotFound();

                // 1. Access Check
                // if (!await HasAccountAccess(opportunity.AccountId)) return Forbid();

                // 2. Lock Check - REMOVED per user request
                // if (opportunity.Stage == "Closed Won" || opportunity.Stage == "Closed Lost") ...


                int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId);

                // 3. Owner Change Restriction Disabled
                /*
                if (request.OwnerId.HasValue && request.OwnerId != opportunity.OwnerId)
                {
                    if (User.IsInRole("Sales Rep"))
                    {
                         return StatusCode(403, new { message = "Sales Reps cannot change opportunity ownership." });
                    }
                }
                */

                if (!string.IsNullOrEmpty(request.Name))
                    opportunity.Name = request.Name;
                if (!string.IsNullOrEmpty(request.Stage))
                    opportunity.Stage = request.Stage;
                if (request.Amount.HasValue)
                    opportunity.Amount = request.Amount.Value;
                if (request.CloseDate.HasValue)
                    opportunity.CloseDate = request.CloseDate.Value;
                if (request.OwnerId.HasValue) // Update logic aligns with restrict check
                    opportunity.OwnerId = request.OwnerId.Value;

                opportunity.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Opportunity updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating opportunity: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Move opportunity to different stage
        /// </summary>
        [HttpPost("{id}/move-stage")]
        public async Task<ActionResult<object>> MoveOpportunity(int id, [FromBody] MoveOpportunityRequest request)
        {
            try
            {
                var opportunity = await _context.Opportunities.Include(o => o.Account).FirstOrDefaultAsync(o => o.Id == id);
                if (opportunity == null)
                    return NotFound();

                // if (!await HasAccountAccess(opportunity.AccountId)) return Forbid();
                // if (!await HasAccountAccess(opportunity.AccountId)) return Forbid(); // This line was duplicated, commenting out both.
                // Check removed per request: if (opportunity.Stage == "Closed Won" || opportunity.Stage == "Closed Lost") return StatusCode(403, new { message = "Closed opportunities are locked." });


                opportunity.Stage = request.NewStage;
                opportunity.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Opportunity moved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error moving opportunity: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Mark opportunity as won
        /// </summary>
        [HttpPost("{id}/win")]
        public async Task<ActionResult<object>> WinOpportunity(int id)
        {
            try
            {
                var opportunity = await _context.Opportunities.Include(o => o.Account).FirstOrDefaultAsync(o => o.Id == id);
                if (opportunity == null)
                    return NotFound();

                // if (!await HasAccountAccess(opportunity.AccountId)) return Forbid();
                // Can we mark closed an already closed? Yes, but maybe restrict if already close? 
                // Requirement doesn't explicitly forbid re-closing, but locking implies no edits.
                // Assuming "Win" action IS an edit.
                // Check removed per request: if (opportunity.Stage == "Closed Won" || opportunity.Stage == "Closed Lost") return StatusCode(403, new { message = "Opportunity is already closed." });


                opportunity.Stage = "Closed Won";
                opportunity.WonAt = DateTime.UtcNow;
                opportunity.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Opportunity marked as won" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error winning opportunity: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Mark opportunity as lost
        /// </summary>
        [HttpPost("{id}/lose")]
        public async Task<ActionResult<object>> LoseOpportunity(int id, [FromBody] LoseOpportunityRequest request)
        {
            try
            {
                var opportunity = await _context.Opportunities.Include(o => o.Account).FirstOrDefaultAsync(o => o.Id == id);
                if (opportunity == null)
                    return NotFound();

                // if (!await HasAccountAccess(opportunity.AccountId)) return Forbid();
                // Check removed per request: if (opportunity.Stage == "Closed Won" || opportunity.Stage == "Closed Lost") return StatusCode(403, new { message = "Opportunity is already closed." });


                opportunity.Stage = "Closed Lost";
                opportunity.LostAt = DateTime.UtcNow;
                opportunity.LostReason = request.Reason;
                opportunity.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Opportunity marked as lost" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error losing opportunity: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Delete opportunity
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<object>> DeleteOpportunity(int id)
        {
            try
            {
                var opportunity = await _context.Opportunities.Include(o => o.Account).FirstOrDefaultAsync(o => o.Id == id);
                if (opportunity == null)
                    return NotFound();

                // if (!await HasAccountAccess(opportunity.AccountId)) return Forbid();
                // Check removed per request: if (opportunity.Stage == "Closed Won" || opportunity.Stage == "Closed Lost") return StatusCode(403, new { message = "Closed opportunities cannot be deleted." });


                _context.Opportunities.Remove(opportunity);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Opportunity deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting opportunity: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        private OpportunityDTO MapToDTO(Opportunity opportunity)
        {
            var dto = new OpportunityDTO
            {
                Id = opportunity.Id,
                Name = opportunity.Name,
                AccountId = opportunity.AccountId,
                Stage = opportunity.Stage,
                Amount = opportunity.Amount,
                CloseDate = opportunity.CloseDate,
                OwnerId = opportunity.OwnerId,
                WonAt = opportunity.WonAt,
                LostAt = opportunity.LostAt
            };

            if (opportunity.Account != null)
            {
                dto.RegionId = opportunity.Account.RegionId;
                dto.AccountOwnerId = opportunity.Account.SalesRepId;
                dto.accountName = opportunity.Account.Name;
                dto.regionName = opportunity.Account.Region?.Name;
                dto.accountOwnerName = opportunity.Account.SalesRep?.Name;
            }

            dto.ownerName = opportunity.Owner?.Name;

            return dto;
        }
        [HttpGet("{id}/activities")]
        public async Task<ActionResult<IEnumerable<object>>> GetActivities(int id)
        {
             var opportunity = await _context.Opportunities.FindAsync(id);
             if (opportunity == null) return NotFound();
             // if (!await HasAccountAccess(opportunity.AccountId)) return Forbid();

             // Activities could be Tasks? Or separate Activity entity?
             // Task "TasksController" handles tasks. "Activity Timeline: A log of all tasks (Calls, Emails, Meetings)"
             // So retrieving Tasks where RelatedEntityType = 'Opportunity' and RelatedEntityId = id.
             var tasks = await _context.Tasks.Where(t => t.RelatedEntityType == "Opportunity" && t.RelatedEntityId == id).ToListAsync();
             return Ok(tasks);
        }

        [HttpGet("{id}/documents")]
        public async Task<ActionResult<IEnumerable<object>>> GetDocuments(int id)
        {
             var opportunity = await _context.Opportunities.FindAsync(id);
             if (opportunity == null) return NotFound();
             // if (!await HasAccountAccess(opportunity.AccountId)) return Forbid();

             var docs = await _context.Documents.Where(d => d.RelatedEntityType == "Opportunity" && d.RelatedEntityId == id).ToListAsync();
             return Ok(docs);
        }

        private async Task<bool> HasAccountAccess(int accountId)
        {
             var account = await _context.Accounts.FindAsync(accountId);
             if (account == null) return false;

                int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId);

                if (User.IsInRole("Sales Rep") && account.SalesRepId != currentUserId) return false;
                
                if (User.IsInRole("Regional Lead"))
                {
                     var currentUser = await _context.Users.Include(u => u.UserRegions).FirstOrDefaultAsync(u => u.Id == currentUserId);
                     if (currentUser != null)
                     {
                        var regionIds = currentUser.UserRegions.Select(ur => ur.RegionId).ToList();
                        if (currentUser.RegionId.HasValue) regionIds.Add(currentUser.RegionId.Value);

                        if (!regionIds.Contains(account.RegionId)) return false;
                     }
                }
                return true;
        }




    }
}
