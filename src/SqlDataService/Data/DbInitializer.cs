using Microsoft.EntityFrameworkCore;
using SqlDataService.Data.Entities;

namespace SqlDataService.Data;

public static class DbInitializer
{
    /// <summary>Inserts the 20 default sensors when the table is empty.</summary>
    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Sensors.AnyAsync(cancellationToken))
            return;

        for (var i = 1; i <= 20; i++)
        {
            db.Sensors.Add(new Sensor
            {
                Id = i,
                DisplayName = $"Sensor {i}",
                Unit = string.Empty,
                Enabled = true,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
