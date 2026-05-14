using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SqlDataService.Data;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(
                Environment.GetEnvironmentVariable("SQLDATA_CONNECTION")
                ?? "Host=localhost;Port=5432;Database=industrial;Username=postgres;Password=postgres")
            .Options;

        return new ApplicationDbContext(options);
    }
}
