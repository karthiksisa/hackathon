namespace FDCS.CRM.Backend.DTOs
{
    public class LeadDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Company { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string Status { get; set; }
        public int? OwnerId { get; set; }
        public string? Source { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ConvertedAt { get; set; }
        public string? ownerName { get; set; }
        public string? regionName { get; set; }
        public int? RegionId { get; set; } // Added for client logic
    }

    public class CreateLeadRequest
    {
        public string Name { get; set; }
        public string? Company { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public int? OwnerId { get; set; }
        public int? RegionId { get; set; } // Added
        public string? Source { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateLeadRequest
    {
        public string? Name { get; set; }
        public string? Status { get; set; }
        public string? Company { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Notes { get; set; }
        public int? OwnerId { get; set; }
        public int? RegionId { get; set; } // Added
    }

    public class ConvertLeadRequest
    {
        public int LeadId { get; set; }
        public string AccountName { get; set; }
        public int RegionId { get; set; }
        public int SalesRepId { get; set; }
    }
}
