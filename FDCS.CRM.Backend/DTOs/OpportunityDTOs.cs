namespace FDCS.CRM.Backend.DTOs
{
    public class OpportunityDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int AccountId { get; set; }
        public string Stage { get; set; }
        public decimal Amount { get; set; }
        public DateTime CloseDate { get; set; }
        public int OwnerId { get; set; }
        public DateTime? WonAt { get; set; }
        public DateTime? LostAt { get; set; }
        public int? RegionId { get; set; } // Derived from Account
        public int? AccountOwnerId { get; set; } // Derived from Account
        public string? ownerName { get; set; }
        public string? accountName { get; set; }
        public string? regionName { get; set; }
        public string? accountOwnerName { get; set; }
    }

    public class CreateOpportunityRequest
    {
        public string Name { get; set; }
        public int AccountId { get; set; }
        public string? Stage { get; set; }
        public decimal Amount { get; set; }
        public DateTime CloseDate { get; set; }
        public int OwnerId { get; set; }
    }

    public class UpdateOpportunityRequest
    {
        public string? Name { get; set; }
        public string? Stage { get; set; }
        public decimal? Amount { get; set; }
        public DateTime? CloseDate { get; set; }
        public int? OwnerId { get; set; }
    }

    public class MoveOpportunityRequest
    {
        public int OpportunityId { get; set; }
        public string NewStage { get; set; }
    }

    public class WinOpportunityRequest
    {
        public int OpportunityId { get; set; }
    }

    public class LoseOpportunityRequest
    {
        public int OpportunityId { get; set; }
        public string? Reason { get; set; }
    }
}
