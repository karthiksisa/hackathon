using Microsoft.EntityFrameworkCore;
using FDCS.CRM.Backend.Models;

namespace FDCS.CRM.Backend.Data
{
    public class CrmDbContext : DbContext
    {
        public CrmDbContext(DbContextOptions<CrmDbContext> options) : base(options) { }

        public DbSet<Region> Regions { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<UserRegion> UserRegions { get; set; } = null!;
        public DbSet<Account> Accounts { get; set; } = null!;
        public DbSet<Lead> Leads { get; set; } = null!;
        public DbSet<Contact> Contacts { get; set; } = null!;
        public DbSet<Opportunity> Opportunities { get; set; } = null!;
        public DbSet<CrmTask> Tasks { get; set; } = null!;
        public DbSet<Document> Documents { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================================
            // regions
            // =========================================
            modelBuilder.Entity<Region>(entity =>
            {
                entity.ToTable("regions");

                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.Name).HasDatabaseName("idx_name");
            });


            // ============================
            // users
            // ============================
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");

                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Name)
                      .HasColumnName("name")
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(e => e.Email)
                      .HasColumnName("email")
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(e => e.PasswordHash)
                      .HasColumnName("password_hash")
                      .HasMaxLength(255);

                entity.Property(e => e.Role)
                      .HasColumnName("role")
                      .IsRequired()
                      .HasDefaultValue("Sales Rep");

                entity.Property(e => e.RegionId).HasColumnName("region_id");

                entity.Property(e => e.AddressLine1).HasColumnName("address_line_1").HasMaxLength(255);
                entity.Property(e => e.AddressLine2).HasColumnName("address_line_2").HasMaxLength(255);
                entity.Property(e => e.City).HasColumnName("city").HasMaxLength(100);
                entity.Property(e => e.State).HasColumnName("state").HasMaxLength(100);
                entity.Property(e => e.ZipCode).HasColumnName("zip_code").HasMaxLength(20);
                entity.Property(e => e.PanNumber).HasColumnName("pan_number").HasMaxLength(20);
                entity.Property(e => e.MobileNumber).HasColumnName("mobile_number").HasMaxLength(20);

                // If User inherits BaseEntity with CreatedAt/UpdatedAt, map them:
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.Property(e => e.IsActive)
                      .HasColumnName("is_active")
                      .HasDefaultValue(true);

                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Email).HasDatabaseName("idx_email");
                entity.HasIndex(e => e.Role).HasDatabaseName("idx_role");
                entity.HasIndex(e => e.RegionId).HasDatabaseName("idx_region_id");

                entity.HasOne(e => e.Region)
                      .WithMany(r => r.Users)
                      .HasForeignKey(e => e.RegionId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ============================
            // user_regions
            // ============================
            modelBuilder.Entity<UserRegion>(entity =>
            {
                entity.ToTable("user_regions");

                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.RegionId).HasColumnName("region_id");
                entity.Property(e => e.AssignedAt).HasColumnName("assigned_at");

                entity.HasOne(ur => ur.User)
                      .WithMany(u => u.UserRegions)
                      .HasForeignKey(ur => ur.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ur => ur.Region)
                      .WithMany(r => r.UserRegions)
                      .HasForeignKey(ur => ur.RegionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(ur => new { ur.UserId, ur.RegionId })
                      .IsUnique()
                      .HasDatabaseName("unique_user_region");

                entity.HasIndex(ur => ur.RegionId).HasDatabaseName("idx_region_id");
            });
            // =========================================
            // accounts
            // =========================================
            modelBuilder.Entity<Account>(entity =>
            {
                entity.ToTable("accounts");

                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.RegionId).HasColumnName("region_id");
                entity.Property(e => e.SalesRepId).HasColumnName("sales_rep_id");

                entity.Property(e => e.Industry).HasColumnName("industry").HasMaxLength(100);

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .IsRequired()
                    .HasDefaultValue("Prospect");

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("created_date")
                    .IsRequired();

                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.HasOne(e => e.Region)
                    .WithMany(r => r.Accounts)
                    .HasForeignKey(e => e.RegionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.SalesRep)
                    .WithMany(u => u.OwnedAccounts)
                    .HasForeignKey(e => e.SalesRepId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.Status).HasDatabaseName("idx_status");
                entity.HasIndex(e => e.RegionId).HasDatabaseName("idx_region_id");
                entity.HasIndex(e => e.SalesRepId).HasDatabaseName("idx_sales_rep_id");
                entity.HasIndex(e => e.CreatedDate).HasDatabaseName("idx_created_date");
            });

            // =========================================
            // leads
            // =========================================
            modelBuilder.Entity<Lead>(entity =>
            {
                entity.ToTable("leads");

                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Company).HasColumnName("company").HasMaxLength(255);
                entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255);
                entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .IsRequired()
                    .HasDefaultValue("New");

                entity.Property(e => e.OwnerId).HasColumnName("owner_id");
                entity.Property(e => e.Source).HasColumnName("source").HasMaxLength(100);
                entity.Property(e => e.Notes).HasColumnName("notes");
                entity.Property(e => e.RegionId).HasColumnName("region_id");

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("created_date")
                    .IsRequired();

                entity.Property(e => e.ConvertedAt).HasColumnName("converted_at");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.HasOne(e => e.Owner)
                    .WithMany(u => u.OwnedLeads)
                    .HasForeignKey(e => e.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.Status).HasDatabaseName("idx_status");
                entity.HasIndex(e => e.OwnerId).HasDatabaseName("idx_owner_id");
                entity.HasIndex(e => e.Email).HasDatabaseName("idx_email");
                entity.HasIndex(e => e.CreatedDate).HasDatabaseName("idx_created_date");

                // From schema: CREATE INDEX idx_leads_owner_status ON leads(owner_id, status, created_date DESC);
                entity.HasIndex(e => new { e.OwnerId, e.Status, e.CreatedDate })
                      .HasDatabaseName("idx_leads_owner_status");
            });

            // =========================================
            // contacts
            // =========================================
            modelBuilder.Entity<Contact>(entity =>
            {
                entity.ToTable("contacts");

                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.AccountId).HasColumnName("account_id");

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255);
                entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
                entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(100);

                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.HasOne(e => e.Account)
                    .WithMany(a => a.Contacts)
                    .HasForeignKey(e => e.AccountId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.AccountId).HasDatabaseName("idx_account_id");
                entity.HasIndex(e => e.Email).HasDatabaseName("idx_email");
            });

            // =========================================
            // opportunities
            // =========================================
            modelBuilder.Entity<Opportunity>(entity =>
            {
                entity.ToTable("opportunities");

                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.AccountId).HasColumnName("account_id");

                entity.Property(e => e.Stage)
                    .HasColumnName("stage")
                    .IsRequired()
                    .HasDefaultValue("Prospecting");

                entity.Property(e => e.Amount)
                    .HasColumnName("amount")
                    .HasPrecision(15, 2)
                    .IsRequired();

                entity.Property(e => e.CloseDate)
                    .HasColumnName("close_date")
                    .IsRequired();

                entity.Property(e => e.OwnerId).HasColumnName("owner_id");

                entity.Property(e => e.WonAt).HasColumnName("won_at");
                entity.Property(e => e.LostAt).HasColumnName("lost_at");
                entity.Property(e => e.LostReason).HasColumnName("lost_reason").HasMaxLength(255);

                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.HasOne(e => e.Account)
                    .WithMany(a => a.Opportunities)
                    .HasForeignKey(e => e.AccountId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Owner)
                    .WithMany(u => u.OwnedOpportunities)
                    .HasForeignKey(e => e.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.Stage).HasDatabaseName("idx_stage");
                entity.HasIndex(e => e.AccountId).HasDatabaseName("idx_account_id");
                entity.HasIndex(e => e.OwnerId).HasDatabaseName("idx_owner_id");
                entity.HasIndex(e => e.CloseDate).HasDatabaseName("idx_close_date");
                entity.HasIndex(e => e.Amount).HasDatabaseName("idx_amount");

                // From schema: CREATE INDEX idx_opportunities_account_stage ON opportunities(account_id, stage, close_date);
                entity.HasIndex(e => new { e.AccountId, e.Stage, e.CloseDate })
                      .HasDatabaseName("idx_opportunities_account_stage");
            });

            // =========================================
            // tasks
            // =========================================
            modelBuilder.Entity<CrmTask>(entity =>
            {
                entity.ToTable("tasks");

                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Subject)
                    .HasColumnName("subject")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.DueDate)
                    .HasColumnName("due_date")
                    .IsRequired();

                entity.Property(e => e.Type)
                    .HasColumnName("type")
                    .IsRequired()
                    .HasDefaultValue("Other");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .IsRequired()
                    .HasDefaultValue("Pending");

                entity.Property(e => e.RelatedEntityType)
                    .HasColumnName("related_entity_type")
                    .IsRequired();

                entity.Property(e => e.RelatedEntityId)
                    .HasColumnName("related_entity_id")
                    .IsRequired();

                entity.Property(e => e.CreatedById).HasColumnName("created_by");
                entity.Property(e => e.AssignedToId).HasColumnName("assigned_to");

                entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
                entity.Property(e => e.Notes).HasColumnName("notes");

                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.HasOne(e => e.CreatedBy)
                    .WithMany(u => u.CreatedTasks)
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.AssignedTo)
                    .WithMany(u => u.AssignedTasks)
                    .HasForeignKey(e => e.AssignedToId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.Status).HasDatabaseName("idx_status");
                entity.HasIndex(e => e.Type).HasDatabaseName("idx_type");
                entity.HasIndex(e => e.DueDate).HasDatabaseName("idx_due_date");

                // idx_related (related_entity_type, related_entity_id)
                entity.HasIndex(e => new { e.RelatedEntityType, e.RelatedEntityId })
                      .HasDatabaseName("idx_related");

                entity.HasIndex(e => e.AssignedToId).HasDatabaseName("idx_assigned_to");
            });

            // =========================================
            // documents
            // =========================================
            modelBuilder.Entity<Document>(entity =>
            {
                entity.ToTable("documents");

                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Type)
                    .HasColumnName("type")
                    .IsRequired();

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .IsRequired()
                    .HasDefaultValue("Draft");

                entity.Property(e => e.UploadedById)
                    .HasColumnName("uploaded_by")
                    .IsRequired();

                entity.Property(e => e.UploadedDate)
                    .HasColumnName("uploaded_date")
                    .IsRequired();

                entity.Property(e => e.RelatedEntityType)
                    .HasColumnName("related_entity_type")
                    .IsRequired();

                entity.Property(e => e.RelatedEntityId)
                    .HasColumnName("related_entity_id")
                    .IsRequired();

                entity.Property(e => e.FileSize).HasColumnName("file_size");
                entity.Property(e => e.FilePath).HasColumnName("file_path").HasMaxLength(500);

                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.HasOne(e => e.UploadedBy)
                    .WithMany(u => u.UploadedDocuments)
                    .HasForeignKey(e => e.UploadedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.Status).HasDatabaseName("idx_status");

                entity.HasIndex(e => new { e.RelatedEntityType, e.RelatedEntityId })
                      .HasDatabaseName("idx_related");

                entity.HasIndex(e => e.UploadedById).HasDatabaseName("idx_uploaded_by");
                entity.HasIndex(e => e.UploadedDate).HasDatabaseName("idx_uploaded_date");
            });

            // ============================
            // audit_logs
            // ============================
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("audit_logs");

                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");

                // BaseEntity defines CreatedAt/UpdatedAt. 
                // audit_logs table has `timestamp` which we map CreatedAt to.
                // It does NOT have updated_at, so we ignore UpdatedAt.
                entity.Property(e => e.CreatedAt).HasColumnName("timestamp");
                entity.Ignore(e => e.UpdatedAt);

                // Map other columns that exist in your schema
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.UserName).HasColumnName("user_name").HasMaxLength(255);

                entity.Property(e => e.Action)
                      .HasColumnName("action")
                      .IsRequired();

                entity.Property(e => e.EntityType)
                      .HasColumnName("entity_type")
                      .IsRequired();

                entity.Property(e => e.EntityId).HasColumnName("entity_id");
                entity.Property(e => e.EntityName).HasColumnName("entity_name").HasMaxLength(255);
                entity.Property(e => e.Details).HasColumnName("details");
                entity.Property(e => e.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasColumnName("user_agent").HasMaxLength(500);

                entity.HasOne(e => e.User)
                      .WithMany(u => u.AuditLogs)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.UserId).HasDatabaseName("idx_user_id");
                entity.HasIndex(e => e.Action).HasDatabaseName("idx_action");
                entity.HasIndex(e => new { e.EntityType, e.EntityId }).HasDatabaseName("idx_entity");

                // We can’t map idx_timestamp without a property. See note below.
            });

        }
    }
}
