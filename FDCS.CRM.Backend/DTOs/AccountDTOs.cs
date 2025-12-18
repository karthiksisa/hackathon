namespace FDCS.CRM.Backend.DTOs
{
    public class AccountDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int RegionId { get; set; }
        public int? SalesRepId { get; set; }
        public string? Industry { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? RegionName { get; set; }
        public string? SalesRepName { get; set; }
    }

    public class CreateAccountRequest
    {
        public string Name { get; set; }
        public int RegionId { get; set; }
        public int? SalesRepId { get; set; }
        public string? Industry { get; set; }
    }

    public class UpdateAccountRequest
    {
        public string? Name { get; set; }
        public string? Status { get; set; }
        public string? Industry { get; set; }
        public int? SalesRepId { get; set; }
    }

    public class ApproveAccountRequest
    {
        public int AccountId { get; set; }
    }

    public class RejectAccountRequest
    {
        public int AccountId { get; set; }
        public string? Reason { get; set; }
    }
}
