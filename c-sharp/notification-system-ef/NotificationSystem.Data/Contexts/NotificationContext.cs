using NotificationSystem.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace NotificationSystem.Data.Contexts
{
    public class NotificationContext : DbContext
    {
        public NotificationContext() { }

        public NotificationContext(DbContextOptions<NotificationContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {}
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<EmailNotification> EmailNotifications { get; set; }
        public DbSet<SmsNotification> SmsNotifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User Configuration
            modelBuilder.Entity<User>(u =>
            {
                u.HasKey(u => u.Id);

                u.Property(u => u.Id)
                    .HasMaxLength(8)
                    .IsRequired();
                
                u.Property(u => u.Name)
                    .HasMaxLength(100)
                    .IsRequired();

                u.Property(u => u.PhoneNumber)
                    .HasMaxLength(10)
                    .IsRequired();

                u.Property(u => u.Email)
                    .IsRequired();
            });

            // Notification Configuration (TPH)
            modelBuilder.Entity<Notification>(n =>
            {
                n.HasKey(n => n.Id);

                n.Property(n => n.Id)
                    .HasMaxLength(8)
                    .IsRequired();

                n.Property(n => n.Message)
                    .IsRequired();

                n.Property(n => n.SentDate)
                    .HasColumnType("timestamp with time zone");

                // TPH Discriminator
                n.HasDiscriminator<string>("NotificationType")
                    .HasValue<EmailNotification>("Email")
                    .HasValue<SmsNotification>("SMS");

                // Relationships
                n.HasOne(n => n.Sender)
                    .WithMany(u => u.SentNotifications)
                    .HasForeignKey(n => n.SenderId)
                    .HasConstraintName("FK_Notification_Sender")
                    .OnDelete(DeleteBehavior.Restrict);

                n.HasOne(n => n.Receiver)
                    .WithMany(u => u.ReceivedNotifications)
                    .HasForeignKey(n => n.ReceiverId)
                    .HasConstraintName("FK_Notification_Receiver")
                    .OnDelete(DeleteBehavior.Restrict);

                n.Property(n => n.SenderId).HasMaxLength(8);
                n.Property(n => n.ReceiverId).HasMaxLength(8);
            });
        }
    }
}