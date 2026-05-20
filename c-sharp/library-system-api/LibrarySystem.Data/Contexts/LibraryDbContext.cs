using Microsoft.EntityFrameworkCore;
using LibrarySystem.Models.Models;

namespace LibrarySystem.Data.Contexts;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options)
    {
    }

    public DbSet<Book> Books { get; set; } = null!;
    public DbSet<Member> Members { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(b => b.BookId);

            entity.Property(b => b.Title)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(b => b.Author)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(b => b.ISBN)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(b => b.ISBN)
                .IsUnique();

            entity.Property(b => b.PublishedYear)
                .IsRequired();

            entity.Property(b => b.AvailableCopies)
                .IsRequired()
                .HasDefaultValue(0);
        });

        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasKey(m => m.MemberId);

            entity.Property(m => m.FullName)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(m => m.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.HasIndex(m => m.Email)
                .IsUnique();

            entity.Property(m => m.PhoneNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(m => m.MembershipDate)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
