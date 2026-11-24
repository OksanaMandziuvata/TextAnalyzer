using Microsoft.EntityFrameworkCore;
using TextAnalyzer.Api.Models;

namespace TextAnalyzer.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<AnalysisTask> Tasks => Set<AnalysisTask>();

    public DbSet<AnalysisResult> Results => Set<AnalysisResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnalysisTask>(entity =>
        {
            entity.ToTable("Tasks");
            entity.HasKey(task => task.Id);

            entity.Property(task => task.FileName)
                .HasMaxLength(255)
                .IsRequired();
            entity.Property(task => task.AnalysisType)
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(task => task.Status)
                .HasMaxLength(30)
                .IsRequired();
            entity.Property(task => task.ErrorMessage)
                .HasMaxLength(2000);
            entity.Property(task => task.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(task => task.StartedAt)
                .HasColumnType("timestamp with time zone");
            entity.Property(task => task.CompletedAt)
                .HasColumnType("timestamp with time zone");

            entity.HasIndex(task => task.CreatedAt);
            entity.HasIndex(task => task.Status);

            entity.HasOne(task => task.Result)
                .WithOne(result => result.Task)
                .HasForeignKey<AnalysisResult>(result => result.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AnalysisResult>(entity =>
        {
            entity.ToTable("Results");
            entity.HasKey(result => result.Id);

            entity.Property(result => result.LongestWord)
                .HasMaxLength(500)
                .IsRequired();
            entity.Property(result => result.WordFrequenciesJson)
                .HasColumnType("jsonb")
                .IsRequired();
        });
    }
}
