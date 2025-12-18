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
    public class AccountsController : ControllerBase
    {
        private readonly CrmDbContext _context;
        private readonly ILogger<AccountsController> _logger;

        public AccountsController(CrmDbContext context, ILogger<AccountsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all accounts with optional filtering
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<AccountDTO>>> GetAccounts(
            [FromQuery] string? status = null,
            [FromQuery] int? regionId = null,
            [FromQuery] int? salesRepId = null)
        {
            try
            {
                var query = _context.Accounts
            .Include(a => a.Region)
            .Include(a => a.SalesRep)
            .AsQueryable();

                int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId);

                if (User.IsInRole("Sales Rep"))
                {
                    query = query.Where(a => a.SalesRepId == currentUserId);
                }
                else if (User.IsInRole("Regional Lead"))
                {
                    // Filter by Regional Lead's regions
                    var currentUser = await _context.Users.Include(u => u.UserRegions).FirstOrDefaultAsync(u => u.Id == currentUserId);
                    if (currentUser != null)
                    {
                        var regionIds = currentUser.UserRegions.Select(ur => ur.RegionId).ToList();
                        if (currentUser.RegionId.HasValue) regionIds.Add(currentUser.RegionId.Value);
                        
                        query = query.Where(a => regionIds.Contains(a.RegionId));
                    }
                }

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(a => a.Status == status);

                if (regionId.HasValue)
                    query = query.Where(a => a.RegionId == regionId);

                if (salesRepId.HasValue)
                    query = query.Where(a => a.SalesRepId == salesRepId);

                var accounts = await query.ToListAsync();
                var dtos = accounts.Select(MapToDTO).ToList();

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting accounts: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Get account by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<AccountDTO>> GetAccount(int id)
        {
            try
            {
                var account = await _context.Accounts.FindAsync(id);
                if (account == null)
                    return NotFound();

                // Removed duplicate FindAsync block

                // RBAC Check
                int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId);

                if (User.IsInRole("Sales Rep") && account.SalesRepId != currentUserId)
                {
                     return StatusCode(403, new { message = "You can only view accounts you own." });
                }
                
                if (User.IsInRole("Regional Lead"))
                {
                     var currentUser = await _context.Users.Include(u => u.UserRegions).FirstOrDefaultAsync(u => u.Id == currentUserId);
                     if (currentUser != null)
                     {
                        var regionIds = currentUser.UserRegions.Select(ur => ur.RegionId).ToList();
                        if (currentUser.RegionId.HasValue) regionIds.Add(currentUser.RegionId.Value);

                        if (!regionIds.Contains(account.RegionId))
                        {
                             return StatusCode(403, new { message = "You can only view accounts in your assigned regions." });
                        }
                     }
                }

                return Ok(MapToDTO(account));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting account: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Create new account
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<AccountDTO>> CreateAccount([FromBody] CreateAccountRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId);

                // Default Status
                string status = "Prospect";

                if (User.IsInRole("Sales Rep"))
                {
                    status = "Pending Approval";
                    request.SalesRepId = currentUserId; // Force ownership
                }
                else if (User.IsInRole("Regional Lead"))
                {
                    // Validate Region
                    var currentUser = await _context.Users.Include(u => u.UserRegions).FirstOrDefaultAsync(u => u.Id == currentUserId);
                     if (currentUser != null)
                     {
                        var regionIds = currentUser.UserRegions.Select(ur => ur.RegionId).ToList();
                        if (currentUser.RegionId.HasValue) regionIds.Add(currentUser.RegionId.Value);

                        if (!regionIds.Contains(request.RegionId))
                        {
                            return BadRequest(new { message = "You can only create accounts in your assigned regions." });
                        }
                     }
                }

                var account = new Account
                {
                    Name = request.Name,
                    RegionId = request.RegionId,
                    SalesRepId = request.SalesRepId,
                    Industry = request.Industry,
                    Status = status,
                    CreatedDate = DateTime.Now
                };

                _context.Accounts.Add(account);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, MapToDTO(account));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating account: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Update account
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<object>> UpdateAccount(int id, [FromBody] UpdateAccountRequest request)
        {
            try
            {
                var account = await _context.Accounts.FindAsync(id);
                if (account == null)
                    return NotFound();

                int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId);

                if (User.IsInRole("Sales Rep"))
                {
                    return StatusCode(403, new { message = "Sales Reps cannot edit core account details." });
                }
                
                if (User.IsInRole("Regional Lead"))
                {
                     var currentUser = await _context.Users.Include(u => u.UserRegions).FirstOrDefaultAsync(u => u.Id == currentUserId);
                     if (currentUser != null)
                     {
                        var regionIds = currentUser.UserRegions.Select(ur => ur.RegionId).ToList();
                        if (currentUser.RegionId.HasValue) regionIds.Add(currentUser.RegionId.Value);

                        if (!regionIds.Contains(account.RegionId))
                        {
                             return StatusCode(403, new { message = "You can only edit accounts in your assigned regions." });
                        }
                     }
                }

                if (!string.IsNullOrEmpty(request.Name))
                    account.Name = request.Name;
                if (!string.IsNullOrEmpty(request.Status))
                    account.Status = request.Status;
                if (!string.IsNullOrEmpty(request.Industry))
                    account.Industry = request.Industry;
                if (request.SalesRepId.HasValue)
                    account.SalesRepId = request.SalesRepId;

                account.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Account updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating account: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Approve account (change status from Pending Approval to Active)
        /// </summary>
        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Super Admin, Regional Lead")]
        public async Task<ActionResult<object>> ApproveAccount(int id)
        {
            try
            {
                var account = await _context.Accounts.FindAsync(id);
                if (account == null)
                    return NotFound();

                if (account.Status != "Pending Approval")
                    return BadRequest(new { message = "Account is not pending approval" });

                account.Status = "Active";
                account.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Account approved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error approving account: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Reject account
        /// </summary>
        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Super Admin, Regional Lead")]
        public async Task<ActionResult<object>> RejectAccount(int id, [FromBody] RejectAccountRequest request)
        {
            try
            {
                var account = await _context.Accounts.FindAsync(id);
                if (account == null)
                    return NotFound();

                // Requirement: "Reject (deleting the request)"
                _context.Accounts.Remove(account);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Account request rejected and deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error rejecting account: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Delete account
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Super Admin, Regional Lead")]
        public async Task<ActionResult<object>> DeleteAccount(int id)
        {
            try
            {
                var account = await _context.Accounts.FindAsync(id);
                if (account == null)
                    return NotFound();

                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId);

                if (User.IsInRole("Regional Lead"))
                {
                     var currentUser = await _context.Users.Include(u => u.UserRegions).FirstOrDefaultAsync(u => u.Id == currentUserId);
                     if (currentUser != null)
                     {
                        var regionIds = currentUser.UserRegions.Select(ur => ur.RegionId).ToList();
                        if (currentUser.RegionId.HasValue) regionIds.Add(currentUser.RegionId.Value);

                        if (!regionIds.Contains(account.RegionId))
                        {
                             return StatusCode(403, new { message = "You can only delete accounts in your assigned regions." });
                        }
                     }
                }

                _context.Accounts.Remove(account);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Account deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting account: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        [HttpGet("{id}/contacts")]
        public async Task<ActionResult<IEnumerable<object>>> GetContacts(int id)
        {
             // Verify access to account first
             // Verify access to account first
             if (!await HasAccountAccess(id)) return Forbid();
             
             // Access verified above

             var contacts = await _context.Contacts.Where(c => c.AccountId == id).ToListAsync();
             // Minimal DTO mapping or return raw if lazy? 
             // Using manual mapping to anonymous or existing DTO if avail.
             // Assuming DTO exists or I should inspect ContactDTOs.
             // I'll return the entities for simplicity unless DTO enforced.
             // Controller usually returns DTOs.
             // I'll check Contact model.
             return Ok(contacts); 
        }

        [HttpGet("{id}/opportunities")]
        public async Task<ActionResult<IEnumerable<object>>> GetOpportunities(int id)
        {
             if (!await HasAccountAccess(id)) return Forbid();
             var opps = await _context.Opportunities.Where(o => o.AccountId == id).ToListAsync();
             return Ok(opps);
        }

        [HttpGet("{id}/tasks")]
        public async Task<ActionResult<IEnumerable<object>>> GetTasks(int id)
        {
             if (!await HasAccountAccess(id)) return Forbid();
             // Tasks related to Account
             var tasks = await _context.Tasks.Where(t => t.RelatedEntityType == "Account" && t.RelatedEntityId == id).ToListAsync();
             return Ok(tasks);
        }

        [HttpGet("{id}/documents")]
        public async Task<ActionResult<IEnumerable<object>>> GetDocuments(int id)
        {
             if (!await HasAccountAccess(id)) return Forbid();
             var docs = await _context.Documents.Where(d => d.RelatedEntityType == "Account" && d.RelatedEntityId == id).ToListAsync();
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

        private AccountDTO MapToDTO(Account account)
        {
            return new AccountDTO
            {
                Id = account.Id,
                Name = account.Name,
                RegionId = account.RegionId,
                SalesRepId = account.SalesRepId,
                Industry = account.Industry,
                Status = account.Status,
                CreatedDate = account.CreatedDate,
                RegionName = account.Region?.Name,
                SalesRepName = account.SalesRep?.Name
            };
        }
    }
}
