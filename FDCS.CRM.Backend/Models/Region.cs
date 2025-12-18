using System.Collections.Generic;

namespace FDCS.CRM.Backend.Models
{
    public class Region : BaseEntity
    {
        public string Name { get; set; } = null!;

        // Navigation properties
        public virtual ICollection<User> Users { get; set; } = new List<User>();

        // REQUIRED for UserRegion mapping
        public virtual ICollection<UserRegion> UserRegions { get; set; } = new List<UserRegion>();

        public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
    }
}
