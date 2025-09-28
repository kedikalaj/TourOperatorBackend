using Microsoft.EntityFrameworkCore;
using TourOperator.Domain.Entities;

namespace TourOperator.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<PricingRecord> PricingRecords { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            mb.Entity<User>().HasIndex(u => u.Email).IsUnique();
            mb.Entity<PricingRecord>().HasIndex(u => u.TourOperatorId);

            var adminId = Guid.Parse("98def145-30b0-424b-92e2-68ded6787dfa");
            var tourOperatorId = Guid.Parse("869b02d4-9e30-40c7-9f25-24ead5bf27a7");
            var fixedDateTime = new DateTime(2025, 1, 1);

            // im seeding 2 users with static values for testing purposes
            mb.Entity<User>().HasData(
                new User
                {
                    Id = adminId,
                    Email = "kedi.admin@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Safepwd@1"),
                    Role = "Admin",
                    CreatedAt = fixedDateTime
                },
                new User
                {
                    Id = tourOperatorId,
                    Email = "kedi.tourop@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Safepwd@2"),
                    Role = "TourOperator",
                    CreatedAt = fixedDateTime
                }
            );


        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            // Suppress PendingModelChangesWarning if it's a false positive
            optionsBuilder.ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }
    }
}
