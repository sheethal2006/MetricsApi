using MetricsApi.Models;
using Microsoft.EntityFrameworkCore;
namespace MetricsApi.Data
{
    public class MetricsDbContext : DbContext
    {
        public MetricsDbContext(DbContextOptions<MetricsDbContext> options) : base(options) { }

        public DbSet<MetricReading> MetricReadings => Set<MetricReading>();
        public DbSet<Alert> Alerts => Set<Alert>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MetricReading>().ToTable("metric_readings");
            modelBuilder.Entity<Alert>().ToTable("alerts");
            base.OnModelCreating(modelBuilder);
        }
    }
}
