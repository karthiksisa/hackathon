namespace FDCS.CRM.Backend.Models
{
    public class Document : BaseEntity
    {
        public string Name { get; set; }
        public string Type { get; set; } = "Proposal"; // Proposal, SOW
        public string Status { get; set; } = "Draft"; // Draft, Sent, Signed, Archived
        public int UploadedById { get; set; }
        public DateTime UploadedDate { get; set; }
        public string RelatedEntityType { get; set; } // Account, Opportunity
        public int RelatedEntityId { get; set; }
        public int? FileSize { get; set; }
        public string? FilePath { get; set; }

        // Navigation properties
        public virtual User UploadedBy { get; set; }
    }
}
