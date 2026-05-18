using System;
using System.Collections.Generic;
using LibrarySystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Data.Contexts;

public partial class LibraryDbContext : DbContext
{
    public LibraryDbContext()
    {
    }

    public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<Book> Books { get; set; }

    public virtual DbSet<Bookcategory> Bookcategories { get; set; }

    public virtual DbSet<Bookcopy> Bookcopies { get; set; }

    public virtual DbSet<Borrowing> Borrowings { get; set; }

    public virtual DbSet<Finecalculation> Finecalculations { get; set; }

    public virtual DbSet<Fineconfiguration> Fineconfigurations { get; set; }

    public virtual DbSet<Member> Members { get; set; }

    public virtual DbSet<Membershiplimit> Membershiplimits { get; set; }

    public virtual DbSet<Password> Passwords { get; set; }

    public virtual DbSet<Return> Returns { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.Adminid).HasName("admins_pkey");

            entity.ToTable("admins");

            entity.HasIndex(e => e.Adminemail, "admins_adminemail_key").IsUnique();

            entity.HasIndex(e => e.Adminphone, "admins_adminphone_key").IsUnique();

            entity.Property(e => e.Adminid).HasColumnName("adminid");
            entity.Property(e => e.Adminemail)
                .HasMaxLength(100)
                .HasColumnName("adminemail");
            entity.Property(e => e.Adminname)
                .HasMaxLength(100)
                .HasColumnName("adminname");
            entity.Property(e => e.Adminphone)
                .HasMaxLength(15)
                .HasColumnName("adminphone");
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.Bookid).HasName("books_pkey");

            entity.ToTable("books");

            entity.Property(e => e.Bookid).HasColumnName("bookid");
            entity.Property(e => e.Bookauthor)
                .HasMaxLength(100)
                .HasColumnName("bookauthor");
            entity.Property(e => e.Bookcategory)
                .HasMaxLength(50)
                .HasColumnName("bookcategory");
            entity.Property(e => e.Bookcontents).HasColumnName("bookcontents");
            entity.Property(e => e.Booktitle)
                .HasMaxLength(200)
                .HasColumnName("booktitle");
            entity.Property(e => e.Categorynumber).HasColumnName("categorynumber");

            entity.HasOne(d => d.CategorynumberNavigation).WithMany(p => p.Books)
                .HasForeignKey(d => d.Categorynumber)
                .HasConstraintName("books_categorynumber_fkey");
        });

        modelBuilder.Entity<Bookcategory>(entity =>
        {
            entity.HasKey(e => e.Categorynumber).HasName("bookcategories_pkey");

            entity.ToTable("bookcategories");

            entity.Property(e => e.Categorynumber).HasColumnName("categorynumber");
            entity.Property(e => e.Categoryname)
                .HasMaxLength(100)
                .HasColumnName("categoryname");
        });

        modelBuilder.Entity<Bookcopy>(entity =>
        {
            entity.HasKey(e => e.Copyid).HasName("bookcopies_pkey");

            entity.ToTable("bookcopies");

            entity.Property(e => e.Copyid).HasColumnName("copyid");
            entity.Property(e => e.Bookid).HasColumnName("bookid");
            entity.Property(e => e.Copycondition)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Good'::character varying")
                .HasColumnName("copycondition");
            entity.Property(e => e.Copystatus)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Available'::character varying")
                .HasColumnName("copystatus");

            entity.HasOne(d => d.Book).WithMany(p => p.Bookcopies)
                .HasForeignKey(d => d.Bookid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("bookcopies_bookid_fkey");
        });

        modelBuilder.Entity<Borrowing>(entity =>
        {
            entity.HasKey(e => e.Borrowid).HasName("borrowings_pkey");

            entity.ToTable("borrowings");

            entity.Property(e => e.Borrowid).HasColumnName("borrowid");
            entity.Property(e => e.Bookid).HasColumnName("bookid");
            entity.Property(e => e.Borrowdate)
                .HasDefaultValueSql("CURRENT_DATE")
                .HasColumnName("borrowdate");
            entity.Property(e => e.Duedate).HasColumnName("duedate");
            entity.Property(e => e.Memberid).HasColumnName("memberid");
            entity.Property(e => e.Remarks)
                .HasMaxLength(200)
                .HasColumnName("remarks");
            entity.Property(e => e.Returnstatus)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Borrowed'::character varying")
                .HasColumnName("returnstatus");

            entity.HasOne(d => d.Book).WithMany(p => p.Borrowings)
                .HasForeignKey(d => d.Bookid)
                .HasConstraintName("borrowings_bookid_fkey");

            entity.HasOne(d => d.Member).WithMany(p => p.Borrowings)
                .HasForeignKey(d => d.Memberid)
                .HasConstraintName("borrowings_memberid_fkey");
        });

        modelBuilder.Entity<Finecalculation>(entity =>
        {
            entity.HasKey(e => e.Fineid).HasName("finecalculation_pkey");

            entity.ToTable("finecalculation");

            entity.Property(e => e.Fineid).HasColumnName("fineid");
            entity.Property(e => e.Borrowid).HasColumnName("borrowid");
            entity.Property(e => e.Fineamount)
                .HasPrecision(10, 2)
                .HasColumnName("fineamount");
            entity.Property(e => e.Finestatus)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Unpaid'::character varying")
                .HasColumnName("finestatus");
            entity.Property(e => e.Remarks)
                .HasMaxLength(200)
                .HasColumnName("remarks");

            entity.HasOne(d => d.Borrow).WithMany(p => p.Finecalculations)
                .HasForeignKey(d => d.Borrowid)
                .HasConstraintName("finecalculation_borrowid_fkey");
        });

        modelBuilder.Entity<Fineconfiguration>(entity =>
        {
            entity.HasKey(e => e.Fineconfigid).HasName("fineconfiguration_pkey");

            entity.ToTable("fineconfiguration");

            entity.HasIndex(e => e.Finetype, "fineconfiguration_finetype_key").IsUnique();

            entity.Property(e => e.Fineconfigid)
                .HasDefaultValueSql("nextval('fineconfiguration_fineid_seq'::regclass)")
                .HasColumnName("fineconfigid");
            entity.Property(e => e.Amount)
                .HasPrecision(10, 2)
                .HasColumnName("amount");
            entity.Property(e => e.Finetype)
                .HasMaxLength(50)
                .HasColumnName("finetype");
        });

        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasKey(e => e.Memberid).HasName("members_pkey");

            entity.ToTable("members");

            entity.HasIndex(e => e.Memberemail, "members_memberemail_key").IsUnique();

            entity.HasIndex(e => e.Memberphone, "members_memberphone_key").IsUnique();

            entity.Property(e => e.Memberid).HasColumnName("memberid");
            entity.Property(e => e.Memberemail)
                .HasMaxLength(100)
                .HasColumnName("memberemail");
            entity.Property(e => e.Membername)
                .HasMaxLength(100)
                .HasColumnName("membername");
            entity.Property(e => e.Memberphone)
                .HasMaxLength(15)
                .HasColumnName("memberphone");
            entity.Property(e => e.Memberstatus)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Active'::character varying")
                .HasColumnName("memberstatus");
            entity.Property(e => e.Membertype)
                .HasMaxLength(20)
                .HasColumnName("membertype");

            entity.HasOne(d => d.MembertypeNavigation).WithMany(p => p.Members)
                .HasForeignKey(d => d.Membertype)
                .HasConstraintName("members_membertype_fkey");
        });

        modelBuilder.Entity<Membershiplimit>(entity =>
        {
            entity.HasKey(e => e.Membertype).HasName("membershiplimits_pkey");

            entity.ToTable("membershiplimits");

            entity.Property(e => e.Membertype)
                .HasMaxLength(20)
                .HasColumnName("membertype");
            entity.Property(e => e.Borrowdurationdays).HasColumnName("borrowdurationdays");
            entity.Property(e => e.Maxbooksallowed).HasColumnName("maxbooksallowed");
        });

        modelBuilder.Entity<Password>(entity =>
        {
            entity.HasKey(e => e.Userid).HasName("passwords_pkey");

            entity.ToTable("passwords");

            entity.Property(e => e.Userid)
                .HasMaxLength(50)
                .HasColumnName("userid");
            entity.Property(e => e.Passwordhash).HasColumnName("passwordhash");
        });

        modelBuilder.Entity<Return>(entity =>
        {
            entity.HasKey(e => e.Returnid).HasName("returns_pkey");

            entity.ToTable("returns");

            entity.Property(e => e.Returnid).HasColumnName("returnid");
            entity.Property(e => e.Actualreturndate)
                .HasDefaultValueSql("CURRENT_DATE")
                .HasColumnName("actualreturndate");
            entity.Property(e => e.Borrowid).HasColumnName("borrowid");
            entity.Property(e => e.Fineamount)
                .HasPrecision(10, 2)
                .HasDefaultValue(0.00m)
                .HasColumnName("fineamount");
            entity.Property(e => e.Returnapprovalstatus)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Pending'::character varying")
                .HasColumnName("returnapprovalstatus");

            entity.HasOne(d => d.Borrow).WithMany(p => p.Returns)
                .HasForeignKey(d => d.Borrowid)
                .HasConstraintName("returns_borrowid_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
