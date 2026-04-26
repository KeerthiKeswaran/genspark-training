using Microsoft.EntityFrameworkCore;
using server.Core.Entities;

namespace server.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<BusOperator> BusOperators { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<BusRoute> Routes { get; set; }
        public DbSet<Bus> Buses { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<SeatLock> SeatLocks { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Passenger> Passengers { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<Hub> Hubs { get; set; }
        public DbSet<PlatformSetting> PlatformSettings { get; set; }
        public DbSet<GlobalConfiguration> GlobalConfigurations { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // TPT (Table-per-Type) Inheritance
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<BusOperator>().ToTable("BusOperators");
            modelBuilder.Entity<Admin>().ToTable("Admins");

            // Indices for duplicate booking prevention and performance
            modelBuilder.Entity<Booking>()
                .HasIndex(b => new { b.CustomerId, b.JourneyId });

            modelBuilder.Entity<SeatLock>()
                .HasIndex(l => new { l.LockedByUserId, l.JourneyId });

            // Booking relationships
            modelBuilder.Entity<Booking>()
                .HasMany(b => b.Passengers)
                .WithOne(p => p.Booking)
                .HasForeignKey(p => p.BookingId);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Payment)
                .WithOne(p => p.Booking)
                .HasForeignKey<Payment>(p => p.BookingId);

            // Hub relationships
            modelBuilder.Entity<Hub>()
                .HasOne(h => h.City)
                .WithMany(c => c.Hubs)
                .HasForeignKey(h => h.CityId);

            // Booking to Hub relationships
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.BoardingHub)
                .WithMany()
                .HasForeignKey(b => b.BoardingHubId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.DroppingHub)
                .WithMany()
                .HasForeignKey(b => b.DroppingHubId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
