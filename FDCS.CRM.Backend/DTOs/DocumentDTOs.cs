namespace FDCS.CRM.Backend.DTOs
{
    public class DocumentDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public int UploadedById { get; set; }
        public DateTime UploadedDate { get; set; }
        public string RelatedEntityType { get; set; }
        public int RelatedEntityId { get; set; }
        public int? FileSize { get; set; }
        public int? RegionId { get; set; }
        public int? OwnerId { get; set; }
        public string? uploadedByName { get; set; }
        public string? regionName { get; set; }
        public string? ownerName { get; set; } // Account Owner Name
    }

    public class CreateDocumentRequest
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string RelatedEntityType { get; set; }
        public int RelatedEntityId { get; set; }
    }

    public class UpdateDocumentRequest
    {
        public string? Status { get; set; }
        public string? Name { get; set; }
    }

    public class DocumentUploadRequest
    {
        public IFormFile File { get; set; }
        public string Type { get; set; }
        public string RelatedEntityType { get; set; }
        public int RelatedEntityId { get; set; }
    }
}
