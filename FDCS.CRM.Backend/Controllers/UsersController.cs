using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FDCS.CRM.Backend.Data;
using FDCS.CRM.Backend.DTOs;
using FDCS.CRM.Backend.Models;
using FDCS.CRM.Backend.Models;
using FDCS.CRM.Backend.Services;
using System.Security.Claims;

namespace FDCS.CRM.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly CrmDbContext _context;
        private readonly IAuthenticationService _authService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(CrmDbContext context, IAuthenticationService authService, ILogger<UsersController> logger)
        {
            _context = context;
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Get all users with optional filtering
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<UserDTO>>> GetUsers(
            [FromQuery] string? role = null,
            [FromQuery] int? regionId = null)
        {
            try
            {
                var query = _context.Users
                    .Include(u => u.UserRegions)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(role))
                    query = query.Where(u => u.Role == role);

                if (regionId.HasValue)
                {
                    // Filter: User is assigned to this region (either primary or secondary)
                    // Note: User.RegionId is primary. UserRegions is secondary.
                    query = query.Where(u => u.RegionId == regionId || u.UserRegions.Any(ur => ur.RegionId == regionId));
                }

                // RBAC for listing users?
                // Super Admin sees all.
                // Regional Lead might only want to see Reps in their region?
                // Requirement allows "filtering users by regionId". 
                // We'll allow authenticated users to list users, simplified for dropdowns.

                var users = await query.ToListAsync();
                var dtos = users.Select(MapToDTO).ToList();

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting users: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDTO>> GetUser(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRegions)
                    .FirstOrDefaultAsync(u => u.Id == id);
                if (user == null)
                    return NotFound();

                return Ok(MapToDTO(user));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Create new user
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Super Admin")]
        public async Task<ActionResult<UserDTO>> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                    return BadRequest(new { message = "Email already exists" });

                var user = new User
                {
                    Name = request.Name,
                    Email = request.Email,
                    PasswordHash = _authService.HashPassword(request.Password),
                    Role = request.Role,
                    RegionId = request.RegionId,
                    MobileNumber = request.MobileNumber,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Log Audit
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var currentUserName = User.FindFirst(ClaimTypes.Name)?.Value;
                int? adminId = int.TryParse(currentUserId, out var parsedId) ? parsedId : null;

                await _authService.LogActivityAsync(adminId, currentUserName, "Create", "User", user.Id, user.Name, $"User {user.Email} created");

                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, MapToDTO(user));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating user: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Update user
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<object>> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound();

                if (!string.IsNullOrEmpty(request.Name))
                    user.Name = request.Name;
                if (!string.IsNullOrEmpty(request.MobileNumber))
                    user.MobileNumber = request.MobileNumber;
                if (!string.IsNullOrEmpty(request.City))
                    user.City = request.City;
                if (!string.IsNullOrEmpty(request.State))
                    user.State = request.State;
                if (request.RegionId.HasValue)
                    user.RegionId = request.RegionId;
                
                if (!string.IsNullOrEmpty(request.Role))
                {
                    // Only Super Admin can change roles
                    var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
                    if (currentUserRole != "Super Admin")
                    {
                        return StatusCode(403, new { message = "Only Super Admin can change user roles." });
                    }
                    user.Role = request.Role;
                }

                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating user: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }



        /// <summary>
        /// Get all region IDs a user has access to (Primary + Secondary)
        /// </summary>
        [HttpGet("{id}/regions")]
        public async Task<ActionResult<List<int>>> GetUserAccessibleRegions(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRegions)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return NotFound();

                // Access Control: Allow Admin or Self?
                // For now, allowing any authenticated user to facilitate UI dropdowns/logic.
                
                var regionIds = new List<int>();
                
                // 1. Primary Region
                if (user.RegionId.HasValue)
                {
                    regionIds.Add(user.RegionId.Value);
                }

                // 2. Secondary Regions
                if (user.UserRegions != null)
                {
                    regionIds.AddRange(user.UserRegions.Select(ur => ur.RegionId));
                }

                return Ok(regionIds.Distinct().ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user regions: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Assign regions to user
        /// </summary>
        [HttpPost("{userId}/regions")]
        [Authorize(Roles = "Super Admin")]
        public async Task<ActionResult<object>> AssignRegions(int userId, [FromBody] UserRegionsRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound();

                // Remove existing region assignments
                var existingRegions = await _context.UserRegions
                    .Where(ur => ur.UserId == userId)
                    .ToListAsync();
                _context.UserRegions.RemoveRange(existingRegions);

                // Add new regions
                foreach (var regionId in request.RegionIds)
                {
                    _context.UserRegions.Add(new UserRegion
                    {
                        UserId = userId,
                        RegionId = regionId
                    });
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Regions assigned successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error assigning regions: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Deactivate user
        /// </summary>
        [HttpPost("{id}/deactivate")]
        [Authorize(Roles = "Super Admin")]
        public async Task<ActionResult<object>> DeactivateUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound();

                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "User deactivated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deactivating user: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Delete user
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Super Admin")]
        public async Task<ActionResult<object>> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound();

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return Ok(new { message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting user: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        private UserDTO MapToDTO(User user)
        {
            return new UserDTO
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                RegionId = user.RegionId,
                City = user.City,
                State = user.State,
                MobileNumber = user.MobileNumber,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                RegionIds = user.UserRegions?.Select(ur => ur.RegionId).ToList() ?? new List<int>()
            };
        }
    }
}
