namespace FDCS.CRM.Backend.DTOs
{
    public class ContactDTO
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Title { get; set; }
    }

    public class CreateContactRequest
    {
        public int AccountId { get; set; }
        public string Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Title { get; set; }
    }

    public class UpdateContactRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Title { get; set; }
    }
}
