namespace FDCS.CRM.Backend.Models
{
    public class Account : BaseEntity
    {
        public string Name { get; set; }
        public int RegionId { get; set; }
        public int? SalesRepId { get; set; }
        public string? Industry { get; set; }
        public string Status { get; set; } = "Prospect"; // Active, Prospect, Inactive, Pending Approval
        public DateTime CreatedDate { get; set; }

        // Navigation properties
        public virtual Region Region { get; set; }
        public virtual User? SalesRep { get; set; }
        public virtual ICollection<Contact> Contacts { get; set; } = new List<Contact>();
        public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
    }
}
