namespace FDCS.CRM.Backend.Models
{
    public class Lead : BaseEntity
    {
        public string Name { get; set; }
        public string? Company { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string Status { get; set; } = "New"; // New, Contacted, Qualified, Disqualified, Nurture, Converted
        public int? OwnerId { get; set; }
        public string? Source { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ConvertedAt { get; set; }
        public int? RegionId { get; set; }

        // Navigation properties
        public virtual User? Owner { get; set; }
        public virtual Region? Region { get; set; }
    }
}
