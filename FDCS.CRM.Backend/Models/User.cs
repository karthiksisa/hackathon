namespace FDCS.CRM.Backend.Models
{
    public class User : BaseEntity
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; } = "Sales Rep"; // Super Admin, Regional Lead, Sales Rep
        public int? RegionId { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? PanNumber { get; set; }
        public string? MobileNumber { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual Region? Region { get; set; }
        public virtual ICollection<UserRegion> UserRegions { get; set; } = new List<UserRegion>();
        public virtual ICollection<Account> OwnedAccounts { get; set; } = new List<Account>();
        public virtual ICollection<Lead> OwnedLeads { get; set; } = new List<Lead>();
        public virtual ICollection<Opportunity> OwnedOpportunities { get; set; } = new List<Opportunity>();
        public virtual ICollection<CrmTask> CreatedTasks { get; set; } = new List<CrmTask>();
        public virtual ICollection<CrmTask> AssignedTasks { get; set; } = new List<CrmTask>();
        public virtual ICollection<Document> UploadedDocuments { get; set; } = new List<Document>();
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
}
