namespace FDCS.CRM.Backend.DTOs
{
    public class UserDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public int? RegionId { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? MobileNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<int> RegionIds { get; set; } = new List<int>();
    }

    public class CreateUserRequest
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public int? RegionId { get; set; }
        public string? MobileNumber { get; set; }
    }

    public class UpdateUserRequest
    {
        public string? Name { get; set; }
        public string? MobileNumber { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public int? RegionId { get; set; }
        public string? Role { get; set; }
    }

    public class UserRegionsRequest
    {
        public int UserId { get; set; }
        public List<int> RegionIds { get; set; }
    }
}
