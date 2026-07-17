using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace MaidAndServantt.Models;

public partial class FypContext : DbContext
{
    public FypContext()
    {
    }

    public FypContext(DbContextOptions<FypContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<Experience> Experiences { get; set; }

    public virtual DbSet<Hiring> Hirings { get; set; }

    public virtual DbSet<Interview> Interviews { get; set; }

    public virtual DbSet<Resignation> Resignations { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Skill> Skills { get; set; }

    public virtual DbSet<Termination> Terminations { get; set; }

    public virtual DbSet<Worker> Workers { get; set; }

    public virtual DbSet<WorkerCategory> WorkerCategories { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {

    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Category__6DB38D4ECE380C09");

            entity.ToTable("Category");

            entity.Property(e => e.CategoryId).HasColumnName("Category_ID");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Category_Name");
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.ClientId).HasName("PK__Client__75A5D7187677A344");

            entity.ToTable("Client");

            entity.HasIndex(e => e.Email, "UQ__Client__A9D1053475032132").IsUnique();

            entity.Property(e => e.ClientId).HasColumnName("Client_ID");
            entity.Property(e => e.Address).HasColumnType("text");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Picture)
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Experience>(entity =>
        {
            entity.HasKey(e => e.ExperienceId).HasName("PK__Experien__177FAF2E7FC174E5");

            entity.ToTable("Experience");

            entity.Property(e => e.ExperienceId).HasColumnName("Experience_ID");
            entity.Property(e => e.Duration)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ExpDetail)
                .HasColumnType("text")
                .HasColumnName("Exp_Detail");
            entity.Property(e => e.WorkAt)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("Work_At");
            entity.Property(e => e.WorkerId).HasColumnName("Worker_ID");
        });

        modelBuilder.Entity<Hiring>(entity =>
        {
            entity.HasKey(e => e.HiringId).HasName("PK__Hiring__888164B9ECA2ABF1");

            entity.ToTable("Hiring");

            entity.Property(e => e.HiringId).HasColumnName("Hiring_id");

            entity.Property(e => e.HiringDecision)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Hiring_Decision");

            entity.Property(e => e.InterviewId).HasColumnName("interview_id");

            entity.Property(e => e.WorkerDecision)
                .HasMaxLength(50)
                .IsUnicode(false);

            // 1. HiringDate ki sahi Configuration:
            entity.Property(e => e.HiringDate)
                .HasColumnType("datetime")
                .HasColumnName("HiringDate");

            // 2. Address ki sahi Configuration (Agar aapki Hiring model class mein Address property hai):
            entity.Property(e => e.Address)
                .HasMaxLength(500) // Ya jitna bhi aapka SQL size hai
                .IsUnicode(false)
                .HasColumnName("Address");

            entity.HasOne(d => d.Interview).WithMany(p => p.Hirings)
                .HasForeignKey(d => d.InterviewId)
                .HasConstraintName("FK__Hiring__intervie__5CD6CB2B");
        });

        modelBuilder.Entity<Interview>(entity =>
        {
            entity.HasKey(e => e.InterviewId).HasName("PK__Intervie__536D721920426972");

            entity.ToTable("Interview");

            entity.Property(e => e.InterviewId).HasColumnName("Interview_ID");
            entity.Property(e => e.Address).HasColumnType("text");
            entity.Property(e => e.ClientId).HasColumnName("Client_ID");
            entity.Property(e => e.InterviewDate)
                .HasColumnType("datetime")
                .HasColumnName("Interview_Date");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.WorkerDecision)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.WorkerId).HasColumnName("Worker_ID");
        });

        modelBuilder.Entity<Resignation>(entity =>
        {
            entity.HasKey(e => e.ResignationId).HasName("PK__Resignat__261E3507D514AECB");

            entity.ToTable("Resignation");

            entity.Property(e => e.ResignationId).HasColumnName("Resignation_ID");
            entity.Property(e => e.InterviewId).HasColumnName("Interview_ID");
            entity.Property(e => e.LastWorkingDate).HasColumnName("Last_Working_Date");
            entity.Property(e => e.ResignationReason)
                .HasColumnType("text")
                .HasColumnName("Resignation_Reason");
            entity.Property(e => e.SubmittedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("Submitted_Date");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__Reviews__F85DA7EB664D6AF3");

            entity.Property(e => e.ReviewId).HasColumnName("Review_ID");
            entity.Property(e => e.Comment).HasColumnType("text");
            entity.Property(e => e.InterviewId).HasColumnName("Interview_ID");
            entity.Property(e => e.ReviewDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.SkillsId).HasName("PK__Skills__7569047C62BF914E");

            entity.Property(e => e.SkillsId).HasColumnName("Skills_ID");
            entity.Property(e => e.CategoryId).HasColumnName("Category_ID");
            entity.Property(e => e.SkillName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Skill_Name");
        });

        modelBuilder.Entity<Termination>(entity =>
        {
            entity.HasKey(e => e.TerminationId).HasName("PK__Terminat__C53DD8718B7F2117");

            entity.ToTable("Termination");

            entity.Property(e => e.TerminationId).HasColumnName("Termination_ID");
            entity.Property(e => e.InterviewId).HasColumnName("Interview_ID");
            entity.Property(e => e.TerminatedDate).HasColumnName("Terminated_Date");
            entity.Property(e => e.TerminatedReason)
                .HasColumnType("text")
                .HasColumnName("Terminated_Reason");
        });

        modelBuilder.Entity<Worker>(entity =>
        {
            entity.HasKey(e => e.WorkerId).HasName("PK__Worker__F35E9FF4D3CD125D");

            entity.ToTable("Worker");

            entity.HasIndex(e => e.Cnic, "UQ__Worker__A29801FA67CCC70F").IsUnique();

            entity.Property(e => e.WorkerId).HasColumnName("Worker_ID");
            entity.Property(e => e.Address).HasColumnType("text");
            entity.Property(e => e.Age).HasColumnName("age");
            entity.Property(e => e.AvailableStatus)
                .HasDefaultValue(true)
                .HasColumnName("Available_Status");
            entity.Property(e => e.Bio)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Cnic)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Number)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("number");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Picture)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Salary).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<WorkerCategory>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("Worker_Category");

            entity.Property(e => e.CategoryId).HasColumnName("Category_id");
            entity.Property(e => e.SkillsId).HasColumnName("Skills_Id");
            entity.Property(e => e.WorkerId).HasColumnName("Worker_id");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
