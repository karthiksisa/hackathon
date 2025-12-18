using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FDCS.CRM.Backend.DTOs;
using FDCS.CRM.Backend.Services;

namespace FDCS.CRM.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthenticationService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// User login - returns JWT token for authentication
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var response = await _authService.LoginAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning($"Login failed: {ex.Message}");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Login error: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        /// <summary>
        /// Get current authenticated user information
        /// </summary>
        [HttpGet("current")]
        [Authorize]
        public async Task<ActionResult<CurrentUserResponse>> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                    return Unauthorized();

                var user = await _authService.GetCurrentUserAsync(userId);
                return Ok(user);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting current user: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Change password for authenticated user
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult<object>> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (request.UserId <= 0)
                    return BadRequest(new { message = "Invalid User ID" });

                await _authService.ChangePasswordAsync(request.UserId, request);
                return Ok(new { message = "Password changed successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error changing password: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Validate JWT token
        /// </summary>
        [HttpPost("validate-token")]
        [AllowAnonymous]
        public async Task<ActionResult<ValidateTokenResponse>> ValidateToken([FromBody] object tokenRequest)
        {
            try
            {
                // Extract token from Authorization header or request body
                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                if (string.IsNullOrEmpty(token))
                {
                    var bodyToken = ((System.Collections.Generic.Dictionary<string, object>)tokenRequest)?
                        .GetValueOrDefault("token")?.ToString();
                    token = bodyToken;
                }

                if (string.IsNullOrEmpty(token))
                    return BadRequest(new { message = "Token is required" });

                var result = await _authService.ValidateTokenAsync(token);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error validating token: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// User logout
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<object>> Logout()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out var userId))
                {
                    await _authService.LogoutAsync(userId);
                }
                return Ok(new { message = "Logout successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during logout: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }
    }
}
