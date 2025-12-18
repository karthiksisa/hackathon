using Bogus;
using FDCS.CRM.Backend.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FDCS.CRM.Backend.Data
{
    public class DataSeeder
    {
        private readonly CrmDbContext _context;

        public DataSeeder(CrmDbContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            // 1. Ensure Regions
            var regions = new List<string> { "India - North", "India - South", "India - East", "India - West" };
            foreach (var rName in regions)
            {
                if (!await _context.Regions.AnyAsync(r => r.Name == rName))
                {
                    _context.Regions.Add(new Region { Name = rName });
                }
            }
            await _context.SaveChangesAsync();
            
            var dbRegions = await _context.Regions.ToListAsync();

            // 2. Ensure Users (Admin, RLs, Reps)
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("password"); // Default password
            var adminPassHash = BCrypt.Net.BCrypt.HashPassword("admin123");

            // Define Core Users to ensure they exist for testing
            var coreUsers = new List<User>
            {
                new User { Name = "Admin User", Email = "admin@fdcs.com", Role = "Super Admin", PasswordHash = adminPassHash },
                new User { Name = "Rajesh Kumar", Email = "rajesh@fdcs.com", Role = "Regional Lead", RegionId = dbRegions.FirstOrDefault(r => r.Name == "India - East")?.Id, PasswordHash = passwordHash },
                new User { Name = "Amit Singh", Email = "amit@fdcs.com", Role = "Sales Rep", RegionId = dbRegions.FirstOrDefault(r => r.Name == "India - East")?.Id, PasswordHash = passwordHash },
                new User { Name = "Sunita Rao", Email = "sunita@fdcs.com", Role = "Sales Rep", RegionId = dbRegions.FirstOrDefault(r => r.Name == "India - South")?.Id, PasswordHash = passwordHash },
                new User { Name = "Priya Patel", Email = "priya@fdcs.com", Role = "Regional Lead", RegionId = dbRegions.FirstOrDefault(r => r.Name == "India - West")?.Id, PasswordHash = passwordHash }
            };

            foreach (var u in coreUsers)
            {
                var existing = await _context.Users.FirstOrDefaultAsync(x => x.Email == u.Email);
                if (existing == null)
                {
                    u.IsActive = true;
                    _context.Users.Add(u);
                }
            }
            await _context.SaveChangesAsync();
            
            // Reload Users to get IDs
            var allUsers = await _context.Users.ToListAsync();
            var salesReps = allUsers.Where(u => u.Role == "Sales Rep").ToList();
            if (!salesReps.Any()) return; // Should not happen

            // 3. Generate Bulk Data (Accounts, Contacts, Opps, Leads)
            if (await _context.Accounts.CountAsync() > 10) 
            {
                return; // Already seeded
            }

            var faker = new Faker("en_IND"); // Indian locale

            // Create 30 Accounts distributed across regions/reps
            var accounts = new List<Account>();
            for (int i = 0; i < 30; i++)
            {
                var rep = faker.PickRandom(salesReps);
                var regionId = rep.RegionId ?? dbRegions.First().Id; // Fallback

                var acc = new Account
                {
                    Name = faker.Company.CompanyName(),
                    Industry = faker.PickRandom(new[] { "Technology", "Manufacturing", "Healthcare", "Finance", "Retail" }),
                    Status = faker.PickRandom(new[] { "Active", "Prospect", "Active", "Active" }), // Bias to Active
                    RegionId = regionId,
                    SalesRepId = rep.Id,
                    CreatedDate = faker.Date.Past(1)
                };
                accounts.Add(acc);
            }
            _context.Accounts.AddRange(accounts);
            await _context.SaveChangesAsync(); // IDs generated

            // Create Contacts (1-3 per account)
            var contacts = new List<Contact>();
            foreach (var acc in accounts)
            {
                int count = faker.Random.Int(1, 3);
                for (int i = 0; i < count; i++)
                {
                    contacts.Add(new Contact
                    {
                        AccountId = acc.Id,
                        Name = faker.Name.FullName(),
                        Email = faker.Internet.Email(),
                        Phone = faker.Phone.PhoneNumber(),
                        Title = faker.Name.JobTitle()
                    });
                }
            }
            _context.Contacts.AddRange(contacts);

            // Create Leads (Unconverted) - 50 leads
            var leads = new List<Lead>();
            for (int i = 0; i < 50; i++)
            {
                var rep = faker.PickRandom(salesReps);
                leads.Add(new Lead
                {
                    Name = faker.Name.FullName(),
                    Company = faker.Company.CompanyName(),
                    Email = faker.Internet.Email(),
                    Status = faker.PickRandom(new[] { "New", "Contacted", "Qualified", "Nurture", "Disqualified" }),
                    OwnerId = rep.Id,
                    Source = faker.PickRandom(new[] { "LinkedIn", "Website", "Referral", "Cold Call" }),
                    CreatedDate = faker.Date.Past(1) // Within last year
                });
            }
            _context.Leads.AddRange(leads);

            // Create Opportunities (The most important for Dashboard)
            // Need mix of Won (Revenue), Open (Pipeline), Lost, Stalled
            var opportunities = new List<Opportunity>();
            var stages = new[] { "Prospecting", "Proposal", "Negotiation", "Closed Won", "Closed Lost" };

            foreach (var acc in accounts)
            {
                int count = faker.Random.Int(1, 5); // Multiple deals per account
                for (int i = 0; i < count; i++)
                {
                    var stage = faker.PickRandom(stages);
                    var created = faker.Date.Past(1); // Created sometime in last year
                    
                    var opp = new Opportunity
                    {
                        Name = $"{faker.Commerce.ProductAdjective()} {faker.Commerce.ProductName()} Deal",
                        AccountId = acc.Id,
                        OwnerId = acc.SalesRepId ?? salesReps.First().Id,
                        Stage = stage,
                        Amount = faker.Random.Decimal(50000, 2000000), // 50k to 20L
                        CreatedAt = created,
                        UpdatedAt = created // Initial
                    };

                    if (stage == "Closed Won")
                    {
                        opp.CloseDate = faker.Date.Between(created, DateTime.Now); // Won in past
                        opp.WonAt = opp.CloseDate;
                    }
                    else if (stage == "Closed Lost")
                    {
                        opp.CloseDate = faker.Date.Between(created, DateTime.Now);
                        opp.LostAt = opp.CloseDate;
                        opp.LostReason = faker.Lorem.Sentence();
                    }
                    else 
                    {
                        // Open Deal
                        opp.CloseDate = faker.Date.Future(3); // Closing in future
                        
                        // Simulate Stalled: UpdatedAt is OLD (> 21 days)
                        // 30% chance of being stalled
                        if (faker.Random.Bool(0.3f))
                        {
                            opp.UpdatedAt = DateTime.UtcNow.AddDays(-faker.Random.Int(25, 60));
                        }
                        else
                        {
                            opp.UpdatedAt = DateTime.UtcNow.AddDays(-faker.Random.Int(0, 10));
                        }
                    }

                    opportunities.Add(opp);
                }
            }
            _context.Opportunities.AddRange(opportunities);

            await _context.SaveChangesAsync();
        }
    }
}
