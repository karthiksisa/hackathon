namespace FDCS.CRM.Backend.DTOs
{
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginResponse
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class ChangePasswordRequest
    {
        public int UserId { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }

    public class CurrentUserResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public int? RegionId { get; set; }
        public string? MobileNumber { get; set; }
        public bool IsActive { get; set; }
    }

    public class ValidateTokenResponse
    {
        public bool IsValid { get; set; }
        public int? UserId { get; set; }
        public string? Message { get; set; }
    }
}
