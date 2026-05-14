using Microsoft.EntityFrameworkCore;
using SqlDataService.Data;

namespace SqlDataService.Tests.TestSupport;

/// <summary>
/// Creates isolated <see cref="ApplicationDbContext"/> instances backed by EF Core InMemory (no PostgreSQL).
/// </summary>
public static class TestDbContextFactory
{
    /// <param name="databaseName">Optional fixed name for debugging; default is a unique database per call.</param>
    public static ApplicationDbContext Create(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName ?? $"SqlDataTests_{Guid.NewGuid():N}")
            .Options;

        var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
