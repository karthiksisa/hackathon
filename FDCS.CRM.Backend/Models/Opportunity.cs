namespace FDCS.CRM.Backend.Models
{
    public class Opportunity : BaseEntity
    {
        public string Name { get; set; }
        public int AccountId { get; set; }
        public string Stage { get; set; } = "Prospecting"; // Prospecting, Proposal, Negotiation, Closed Won, Closed Lost
        public decimal Amount { get; set; }
        public DateTime CloseDate { get; set; }
        public int OwnerId { get; set; }
        public DateTime? WonAt { get; set; }
        public DateTime? LostAt { get; set; }
        public string? LostReason { get; set; }

        // Navigation properties
        public virtual Account Account { get; set; }
        public virtual User Owner { get; set; }
    }
}
