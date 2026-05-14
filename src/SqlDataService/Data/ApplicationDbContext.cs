using Microsoft.EntityFrameworkCore;
using SqlDataService.Data.Entities;

namespace SqlDataService.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Sensor> Sensors => Set<Sensor>();

    public DbSet<TelemetryReading> TelemetryReadings => Set<TelemetryReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Sensor>(e =>
        {
            e.ToTable("sensors");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).ValueGeneratedNever();
            e.Property(s => s.DisplayName).HasMaxLength(256).IsRequired();
            e.Property(s => s.Unit).HasMaxLength(64).IsRequired();
        });

        modelBuilder.Entity<TelemetryReading>(e =>
        {
            e.ToTable("telemetry_readings");
            e.HasKey(t => t.Id);
            e.Property(t => t.Unit).HasMaxLength(64).IsRequired();
            e.Property(t => t.CapturedAtUtc).HasColumnName("captured_at_utc");
            e.HasIndex(t => new { t.SensorId, t.CapturedAtUtc });
            e.HasOne(t => t.Sensor)
                .WithMany(s => s.Readings)
                .HasForeignKey(t => t.SensorId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
