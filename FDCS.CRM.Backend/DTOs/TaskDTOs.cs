namespace FDCS.CRM.Backend.DTOs
{
    public class CrmTaskDTO
    {
        public int Id { get; set; }
        public string Subject { get; set; }
        public DateTime DueDate { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string RelatedEntityType { get; set; }
        public int RelatedEntityId { get; set; }
        public int? AssignedToId { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Notes { get; set; }
        public int? RegionId { get; set; } // Derived
        public int? RelatedOwnerId { get; set; } // Derived
    }

    public class CreateTaskRequest
    {
        public string Subject { get; set; }
        public DateTime DueDate { get; set; }
        public string Type { get; set; }
        public string RelatedEntityType { get; set; }
        public int RelatedEntityId { get; set; }
        public int? AssignedToId { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateTaskRequest
    {
        public string? Subject { get; set; }
        public string? Status { get; set; }
        public DateTime? DueDate { get; set; }
        public int? AssignedToId { get; set; }
        public string? Notes { get; set; }
    }

    public class CompleteTaskRequest
    {
        public int TaskId { get; set; }
    }

    public class HealthCheckRequest
    {
        public bool Default { get; set; }
        public string? Doomsday { get; set; }
        public string? Type { get; set; } 
    }
}
