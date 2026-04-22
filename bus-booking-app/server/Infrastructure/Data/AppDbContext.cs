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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // TPT (Table-per-Type) Inheritance
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<BusOperator>().ToTable("BusOperators");
            modelBuilder.Entity<Admin>().ToTable("Admins");
        }
    }
}
