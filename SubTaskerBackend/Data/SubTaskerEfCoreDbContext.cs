using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SubTaskerBackend.Models;

namespace SubTaskerBackend.Data
{
    public class SubTaskerEfCoreDbContext : DbContext
    {
        public SubTaskerEfCoreDbContext(DbContextOptions<SubTaskerEfCoreDbContext> options) : base(options)
        {
        }

        public DbSet<TaskItems> TaskItems { get; set; } = null!;
        public DbSet<Categories> Categories { get; set; } = null!;
        public DbSet<Tags> Tags { get; set; } = null!;
        public DbSet<Users> Users { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the Users entity
            modelBuilder.Entity<Users>(entity =>
            {
                entity.Property(e => e.Username)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.HasIndex(e => e.Username)
                    .IsUnique();

                entity.Property(e => e.Email)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.HasIndex(e => e.Email)
                    .IsUnique();

                entity.Property(e => e.PasswordHash)
                    .HasColumnType("text")
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .IsRequired();

                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("timestamp with time zone");
            });

            // Configure the Categories entity
            modelBuilder.Entity<Categories>(entity =>
            {
                entity.Property(e => e.Name)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.HasIndex(e => e.Name)
                    .IsUnique();

                entity.Property(e => e.UserId)
                    .IsRequired();

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .IsRequired();

                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("timestamp with time zone");
            });

            // Configure the Tags entity
            modelBuilder.Entity<Tags>(entity =>
            {
                entity.Property(e => e.Name)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.HasIndex(e => e.Name)
                    .IsUnique();

                entity.Property(e => e.UserId)
                    .IsRequired();

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .IsRequired();

                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("timestamp with time zone");
            });

            // Configure the TaskItems entity
            modelBuilder.Entity<TaskItems>(entity =>
            {
                entity.Property(e => e.Title)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.Description)
                    .HasColumnType("text");

                entity.Property(e => e.Status)
                    .HasConversion<int>()
                    .HasDefaultValueSql("0")
                    .IsRequired();

                entity.Property(e => e.Priority)
                    .HasConversion<int>()
                    .HasDefaultValueSql("1")
                    .IsRequired();

                entity.Property(e => e.DueDate)
                    .HasColumnType("timestamp with time zone");

                entity.HasOne(e => e.Category)
                    .WithMany()
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.ParentTask)
                    .WithMany(e => e.SubTasks)
                    .HasForeignKey(e => e.ParentTaskId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(e => e.UserId)
                    .IsRequired();

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Tags)
                    .WithMany(e => e.Tasks)
                    .UsingEntity<Dictionary<string, object>>(
                        "TaskItemsTags",
                        j => j
                            .HasOne<Tags>()
                            .WithMany()
                            .HasForeignKey("TagId")
                            .OnDelete(DeleteBehavior.Cascade),
                        j => j
                            .HasOne<TaskItems>()
                            .WithMany()
                            .HasForeignKey("TaskId")
                            .OnDelete(DeleteBehavior.Cascade),
                        j =>
                        {
                            j.HasKey("TaskId", "TagId");
                            j.ToTable("TaskItemsTags");
                        });

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .IsRequired();

                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("timestamp with time zone");
            });
        }
    }
}