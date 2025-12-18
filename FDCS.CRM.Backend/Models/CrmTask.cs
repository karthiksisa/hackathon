namespace FDCS.CRM.Backend.Models
{
    public class CrmTask : BaseEntity
    {
        public string Subject { get; set; }
        public DateTime DueDate { get; set; }
        public string Type { get; set; } = "Other"; // Call, Email, Follow-up, Meeting, Other
        public string Status { get; set; } = "Pending"; // Pending, In Progress, Completed
        public string RelatedEntityType { get; set; } // Lead, Account, Opportunity
        public int RelatedEntityId { get; set; }
        public int? CreatedById { get; set; }
        public int? AssignedToId { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Notes { get; set; }

        // Navigation properties
        public virtual User? CreatedBy { get; set; }
        public virtual User? AssignedTo { get; set; }
    }
}
