namespace FDCS.CRM.Backend.Models
{
    public class UserRegion
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int RegionId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; }
        public virtual Region Region { get; set; }
    }
}
