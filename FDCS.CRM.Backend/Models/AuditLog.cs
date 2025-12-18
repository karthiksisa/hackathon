namespace FDCS.CRM.Backend.Models
{
    public class AuditLog : BaseEntity
    {
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public string Action { get; set; } // Create, Update, Delete, Convert, Login, Logout, Complete, Approve, Reject
        public string EntityType { get; set; } // Lead, Account, Contact, Opportunity, Task, Document, User, Region, System
        public int? EntityId { get; set; }
        public string? EntityName { get; set; }
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        // Navigation properties
        public virtual User? User { get; set; }
    }
}
