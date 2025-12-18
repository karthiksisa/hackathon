using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FDCS.CRM.Backend.Data
{
    public class CrmDbContextFactory : IDesignTimeDbContextFactory<CrmDbContext>
    {
        public CrmDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<CrmDbContext>();
            
            // Read connection string from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            optionsBuilder.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString)
            );

            return new CrmDbContext(optionsBuilder.Options);
        }
    }
}
