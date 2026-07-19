using ConstructionProjectTracker.API.Entities;
using ConstructionProjectTracker.API.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ConstructionProjectTracker.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Engineer> Engineers => Set<Engineer>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectAssignment> ProjectAssignments => Set<ProjectAssignment>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<TaskCompletionReport> TaskCompletionReports => Set<TaskCompletionReport>();
    public DbSet<TaskCompletionApprovalHistory> TaskCompletionApprovalHistories => Set<TaskCompletionApprovalHistory>();
    public DbSet<TaskProgressLog> TaskProgressLogs => Set<TaskProgressLog>();
    public DbSet<TaskDependency> TaskDependencies => Set<TaskDependency>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<TaskDeadlineExtensionRequest> TaskDeadlineExtensionRequests => Set<TaskDeadlineExtensionRequest>();
    public DbSet<ProjectDeadlineExtensionRequest> ProjectDeadlineExtensionRequests => Set<ProjectDeadlineExtensionRequest>();
    public DbSet<TaskDeadlineHistory> TaskDeadlineHistories => Set<TaskDeadlineHistory>();
    public DbSet<ProjectDeadlineHistory> ProjectDeadlineHistories => Set<ProjectDeadlineHistory>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditAndDefaults();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditAndDefaults();
        return base.SaveChanges();
    }

    private void ApplyAuditAndDefaults()
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    ApplyCreatedDefaults(entry, utcNow);
                    break;
                case EntityState.Modified:
                    ApplyUpdatedAt(entry, utcNow);
                    break;
            }
        }
    }

    private static void ApplyCreatedDefaults(EntityEntry entry, DateTime utcNow)
    {
        SetIfDefault(entry, nameof(User.IsActive), true);
        SetIfDefault(entry, nameof(Project.ProgressPercentage), 0);
        SetIfDefault(entry, nameof(TaskItem.CompletionPercentage), 0);
        SetIfDefault(entry, nameof(Project.Status), ProjectStatus.NotStarted);
        SetIfDefault(entry, nameof(TaskItem.Status), ConstructionProjectTracker.API.Enums.TaskStatus.NotStarted);
        SetIfDefault(entry, nameof(Document.UploadDate), utcNow);

        if (entry.Properties.Any(p => p.Metadata.Name == nameof(TaskProgressLog.CreatedAt)))
        {
            var createdAt = entry.Property(nameof(TaskProgressLog.CreatedAt));
            if (createdAt.CurrentValue is DateTime logCreated && logCreated == default)
                createdAt.CurrentValue = utcNow;
        }

        if (entry.Properties.Any(p => p.Metadata.Name == nameof(User.CreatedAt)))
        {
            var createdAt = entry.Property(nameof(User.CreatedAt));
            if (createdAt.CurrentValue is DateTime created && created == default)
                createdAt.CurrentValue = utcNow;
        }

        if (entry.Properties.Any(p => p.Metadata.Name == nameof(Project.CreatedAt)))
        {
            var createdAt = entry.Property(nameof(Project.CreatedAt));
            if (createdAt.CurrentValue is DateTime created && created == default)
                createdAt.CurrentValue = utcNow;
        }

        ApplyUpdatedAt(entry, utcNow);
    }

    private static void ApplyUpdatedAt(EntityEntry entry, DateTime utcNow)
    {
        var updatedAtProperty = entry.Properties
            .FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");

        if (updatedAtProperty is not null)
            updatedAtProperty.CurrentValue = utcNow;
    }

    private static void SetIfDefault<T>(EntityEntry entry, string propertyName, T value)
    {
        var property = entry.Properties.FirstOrDefault(p => p.Metadata.Name == propertyName);
        if (property is null)
            return;

        if (property.CurrentValue is T current && EqualityComparer<T>.Default.Equals(current, default!))
            property.CurrentValue = value;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Role).HasConversion<string>().HasMaxLength(50);
        });

        modelBuilder.Entity<Engineer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Position).HasMaxLength(100);

            entity.HasOne(e => e.User)
                .WithOne(u => u.Engineer)
                .HasForeignKey<Engineer>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Budget).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
        });

        modelBuilder.Entity<ProjectAssignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ProjectId, e.EngineerId }).IsUnique();

            entity.HasOne(e => e.Project)
                .WithMany(p => p.ProjectAssignments)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Engineer)
                .WithMany(en => en.ProjectAssignments)
                .HasForeignKey(e => e.EngineerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Priority).HasConversion<string>().HasMaxLength(50);

            entity.HasOne(e => e.Project)
                .WithMany(p => p.Tasks)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.AssignedEngineer)
                .WithMany(en => en.AssignedTasks)
                .HasForeignKey(e => e.AssignedEngineerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.CompletionReport)
                .WithOne(r => r.Task)
                .HasForeignKey<TaskCompletionReport>(r => r.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.ProgressLogs)
                .WithOne(l => l.Task)
                .HasForeignKey(l => l.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskDependency>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TaskId, e.DependsOnTaskId }).IsUnique();

            entity.HasOne(e => e.Task)
                .WithMany(t => t.Dependencies)
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.DependsOnTask)
                .WithMany(t => t.DependentTasks)
                .HasForeignKey(e => e.DependsOnTaskId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TaskProgressLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasMaxLength(2000).IsRequired();
            entity.HasIndex(e => new { e.TaskId, e.CreatedAt });

            entity.HasOne(e => e.Engineer)
                .WithMany(en => en.ProgressLogs)
                .HasForeignKey(e => e.EngineerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TaskCompletionReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TaskId).IsUnique();
            entity.Property(e => e.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.StoredFileName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Extension).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.RelativeFilePath).HasMaxLength(500).IsRequired();
            entity.Property(e => e.RejectionComment).HasMaxLength(1000);

            entity.HasOne(e => e.UploadedByUser)
                .WithMany(u => u.UploadedCompletionReports)
                .HasForeignKey(e => e.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ReviewedByUser)
                .WithMany(u => u.ReviewedCompletionReports)
                .HasForeignKey(e => e.ReviewedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.ApprovalHistory)
                .WithOne(h => h.TaskCompletionReport)
                .HasForeignKey(h => h.TaskCompletionReportId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskCompletionApprovalHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).HasMaxLength(32).IsRequired();
            entity.Property(e => e.RejectionReason).HasMaxLength(1000);
            entity.HasIndex(e => new { e.TaskCompletionReportId, e.ReviewedAt });

            entity.HasOne(e => e.ReviewedByUser)
                .WithMany()
                .HasForeignKey(e => e.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.StoredFileName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Extension).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(50).IsRequired().HasDefaultValue("Other");
            entity.Property(e => e.RelativeFilePath).HasMaxLength(500).IsRequired();

            entity.HasOne(e => e.Project)
                .WithMany(p => p.Documents)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.UploadedByUser)
                .WithMany(u => u.UploadedDocuments)
                .HasForeignKey(e => e.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.RelatedEntityType).HasMaxLength(50);
            entity.HasIndex(e => new { e.UserId, e.IsRead, e.CreatedAt });

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.EntityType).HasMaxLength(50);
            entity.HasIndex(e => e.PerformedAt);

            entity.HasOne(e => e.PerformedByUser)
                .WithMany()
                .HasForeignKey(e => e.PerformedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TaskDeadlineExtensionRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.AdminComment).HasMaxLength(2000);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(e => new { e.TaskId, e.Status });

            entity.HasOne(e => e.Task)
                .WithMany()
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.RequestedByUser)
                .WithMany()
                .HasForeignKey(e => e.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ReviewedByUser)
                .WithMany()
                .HasForeignKey(e => e.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProjectDeadlineExtensionRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.AdminComment).HasMaxLength(2000);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(e => new { e.ProjectId, e.Status });

            entity.HasOne(e => e.Project)
                .WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.RequestedByUser)
                .WithMany()
                .HasForeignKey(e => e.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ReviewedByUser)
                .WithMany()
                .HasForeignKey(e => e.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TaskDeadlineHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).HasMaxLength(2000).IsRequired();
            entity.HasIndex(e => new { e.TaskId, e.ChangedAt });

            entity.HasOne(e => e.Task)
                .WithMany()
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ChangedByUser)
                .WithMany()
                .HasForeignKey(e => e.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProjectDeadlineHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).HasMaxLength(2000).IsRequired();
            entity.HasIndex(e => new { e.ProjectId, e.ChangedAt });

            entity.HasOne(e => e.Project)
                .WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ChangedByUser)
                .WithMany()
                .HasForeignKey(e => e.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
