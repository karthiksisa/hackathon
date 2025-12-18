namespace FDCS.CRM.Backend.Models
{
    public class Contact : BaseEntity
    {
        public int AccountId { get; set; }
        public string Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Title { get; set; }

        // Navigation properties
        public virtual Account Account { get; set; }
    }
}
