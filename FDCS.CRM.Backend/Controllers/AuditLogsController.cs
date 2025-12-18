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
    public class AuditLogsController : ControllerBase
    {
        private readonly CrmDbContext _context;
        private readonly ILogger<AuditLogsController> _logger;

        public AuditLogsController(CrmDbContext context, ILogger<AuditLogsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all audit logs with optional filtering
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<AuditLogDTO>>> GetAuditLogs(
            [FromQuery] int? userId = null,
            [FromQuery] string? action = null,
            [FromQuery] string? entityType = null,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null)
        {
            try
            {
                var query = _context.AuditLogs.AsQueryable();

                if (userId.HasValue)
                    query = query.Where(al => al.UserId == userId);

                if (!string.IsNullOrEmpty(action))
                    query = query.Where(al => al.Action == action);

                if (!string.IsNullOrEmpty(entityType))
                    query = query.Where(al => al.EntityType == entityType);

                if (dateFrom.HasValue)
                    query = query.Where(al => al.CreatedAt >= dateFrom);

                if (dateTo.HasValue)
                    query = query.Where(al => al.CreatedAt <= dateTo);

                var logs = await query.OrderByDescending(al => al.CreatedAt).ToListAsync();
                var dtos = logs.Select(MapToDTO).ToList();

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting audit logs: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Get audit log by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<AuditLogDTO>> GetAuditLog(int id)
        {
            try
            {
                var log = await _context.AuditLogs.FindAsync(id);
                if (log == null)
                    return NotFound();

                return Ok(MapToDTO(log));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting audit log: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        private AuditLogDTO MapToDTO(AuditLog log)
        {
            return new AuditLogDTO
            {
                Id = log.Id,
                Timestamp = log.CreatedAt,
                UserId = log.UserId,
                UserName = log.UserName,
                Action = log.Action,
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                EntityName = log.EntityName,
                Details = log.Details
            };
        }
    }
}
