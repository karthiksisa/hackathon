namespace FDCS.CRM.Backend.DTOs
{
    public class AuditLogDTO
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public int? EntityId { get; set; }
        public string? EntityName { get; set; }
        public string? Details { get; set; }
    }
}
