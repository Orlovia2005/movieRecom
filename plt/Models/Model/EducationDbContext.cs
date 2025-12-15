using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.AspNetCore.Http;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace plt.Models.Model
{
    public partial class EducationDbContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public EducationDbContext(DbContextOptions<EducationDbContext> options,
                                IHttpContextAccessor httpContextAccessor,
                                IConfiguration configuration)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        // Ваши модели
        public virtual DbSet<User> Users { get; set; }
        

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var connectionString = "Host=localhost;Port=5432;Database=movieRecom;Username=postgres;Password=Ignat2005;";
                optionsBuilder.UseNpgsql(connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            // Конфигурация User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("users_pkey");

                entity.HasIndex(e => e.Email)
                    .IsUnique()
                    .HasDatabaseName("ix_users_email");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("name");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("email");

                entity.Property(e => e.Password)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("password");

                entity.Property(e => e.RoleId)
                    .HasColumnName("role_id");

                entity.HasOne(u => u.Role)
                      .WithMany()
                      .HasForeignKey(u => u.RoleId)
                      .HasConstraintName("fk_users_roles_role_id")
                      .OnDelete(DeleteBehavior.Restrict);
            });

            

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var auditEntries = OnBeforeSaveChanges();
            var result = base.SaveChangesAsync(cancellationToken);
            OnAfterSaveChanges(auditEntries);
            return result;
        }

        public override int SaveChanges()
        {
            var auditEntries = OnBeforeSaveChanges();
            var result = base.SaveChanges();
            OnAfterSaveChanges(auditEntries);
            return result;
        }

        private List<AuditEntry> OnBeforeSaveChanges()
        {
            ChangeTracker.DetectChanges();
            var auditEntries = new List<AuditEntry>();
            var userId = GetCurrentUserId();

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
                {
                    var auditEntry = new AuditEntry(entry)
                    {
                        TableName = entry.Metadata.GetTableName() ?? "Unknown",
                        Action = entry.State.ToString(),
                        UserId = userId,
                    };

                    foreach (var property in entry.Properties)
                    {
                        string propertyName = property.Metadata.Name;
                        auditEntry.NewValues[propertyName] = property.CurrentValue ?? "NULL";
                    }

                    auditEntries.Add(auditEntry);
                }
            }

            return auditEntries;
        }

        private void OnAfterSaveChanges(List<AuditEntry> auditEntries)
        {
            if (!auditEntries.Any())
                return;

            var ip = _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";

            foreach (var auditEntry in auditEntries)
            {
                Log.Information("Изменения в таблице {TableName}, действие {Action}, пользователь {UserId}, IP {IP}, новые значения: {Value}",
                    auditEntry.TableName,
                    auditEntry.Action,
                    auditEntry.UserId,
                    ip,
                    auditEntry.NewValues);
            }
        }

        private string GetCurrentUserId()
        {
            return _httpContextAccessor?.HttpContext?.User?.FindFirstValue("Id") ?? "Unauthorized";
        }
    }

    public class AuditEntry
    {
        public string TableName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public Dictionary<string, object> NewValues { get; set; } = new Dictionary<string, object>();

        public AuditEntry(EntityEntry entry)
        {
            // Конструктор для инициализации
        }
    }
}