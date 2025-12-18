namespace FDCS.CRM.Backend.DTOs
{
    public class RegionDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateRegionRequest
    {
        public string Name { get; set; }
    }

    public class UpdateRegionRequest
    {
        public string Name { get; set; }
    }
}
