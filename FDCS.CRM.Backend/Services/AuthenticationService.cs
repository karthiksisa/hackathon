using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using FDCS.CRM.Backend.Data;
using FDCS.CRM.Backend.DTOs;
using FDCS.CRM.Backend.Models;
using System.Security.Cryptography;

namespace FDCS.CRM.Backend.Services
{
    public interface IAuthenticationService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<CurrentUserResponse> GetCurrentUserAsync(int userId);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request);
        Task LogoutAsync(int userId);
        Task<ValidateTokenResponse> ValidateTokenAsync(string token);
        string GenerateJwtToken(User user);
        string HashPassword(string password);
        Task LogActivityAsync(int? userId, string? userName, string action, string entityType, int? entityId, string? entityName, string? details);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly CrmDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(CrmDbContext context, IConfiguration configuration, ILogger<AuthenticationService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                
                if (user == null || !VerifyPasswordHash(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning($"Login failed for email: {request.Email}");
                    throw new UnauthorizedAccessException("Invalid email or password");
                }

                if (!user.IsActive)
                {
                    throw new UnauthorizedAccessException("User account is inactive");
                }

                var token = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();

                // Log login
                await LogActivityAsync(user.Id, user.Name, "Login", "System", null, null, "User logged in");

                return new LoginResponse
                {
                    UserId = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Role = user.Role,
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60"))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Login error: {ex.Message}");
                throw;
            }
        }

        public async Task<CurrentUserResponse> GetCurrentUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
                throw new KeyNotFoundException("User not found");

            return new CurrentUserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                RegionId = user.RegionId,
                MobileNumber = user.MobileNumber,
                IsActive = user.IsActive
            };
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
                throw new KeyNotFoundException("User not found");

            

            user.PasswordHash = HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            await LogActivityAsync(userId, user.Name, "Update", "System", null, null, "Password changed");
            
            return true;
        }

        public async Task LogoutAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            await LogActivityAsync(userId, user?.Name, "Logout", "System", null, null, "User logged out");
        }

        public async Task<ValidateTokenResponse> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"]);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

                return new ValidateTokenResponse
                {
                    IsValid = true,
                    UserId = int.TryParse(userIdClaim, out var id) ? id : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Token validation failed: {ex.Message}");
                return new ValidateTokenResponse
                {
                    IsValid = false,
                    Message = "Invalid token"
                };
            }
        }

        public string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"]);
            var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        public string HashPassword(string password)
        {
            // MySQL SHA2('x', 256) returns hex string (lowercase by default)
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hashBytes = sha.ComputeHash(bytes);

            // Convert to lowercase hex
            var sb = new StringBuilder(hashBytes.Length * 2);
            foreach (var b in hashBytes)
                sb.Append(b.ToString("x2"));

            return sb.ToString();
        }


        private bool VerifyPasswordHash(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash))
                return false;

            var computed = HashPassword(password);

            // Constant-time compare to avoid timing attacks
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computed),
                Encoding.UTF8.GetBytes(storedHash.Trim())
            );
        }


        public async Task LogActivityAsync(int? userId, string? userName, string action, string entityType, int? entityId, string? entityName, string? details)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                UserName = userName,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                EntityName = entityName,
                Details = details,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
    }
}
