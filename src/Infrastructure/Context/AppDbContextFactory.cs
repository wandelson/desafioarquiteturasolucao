using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Context
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            // Connection string usada para migrations
            optionsBuilder.UseNpgsql(
                "Host=localhost;Port=5432;Database=lancamento-db;Username=postgres;Password=postgres"
            );

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}