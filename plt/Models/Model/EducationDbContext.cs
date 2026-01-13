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

namespace movieRecom.Models.Model
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

        // DbSets
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Movie> Movies { get; set; }
        public virtual DbSet<Genre> Genres { get; set; }
        public virtual DbSet<MovieGenre> MovieGenres { get; set; }
        public virtual DbSet<Rating> Ratings { get; set; }
        public virtual DbSet<Wishlist> Wishlists { get; set; }
        public virtual DbSet<Comment> Comments { get; set; }
        public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
        public virtual DbSet<HiddenRecommendation> HiddenRecommendations { get; set; }

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
            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("users_pkey");
                entity.ToTable("users");

                entity.HasIndex(e => e.Email)
                    .IsUnique()
                    .HasDatabaseName("ix_users_email");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("name");

                entity.Property(e => e.LastName)
                    .HasMaxLength(100)
                    .HasColumnName("last_name");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("email");

                entity.Property(e => e.Password)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("password");

                entity.Property(e => e.AvatarUrl)
                    .HasMaxLength(500)
                    .HasColumnName("avatar_url");

                entity.Property(e => e.Role)
                    .HasColumnName("role")
                    .HasDefaultValue(UserRole.User);
            });

            // Genre configuration
            modelBuilder.Entity<Genre>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("genres");

                entity.HasIndex(e => e.Name)
                    .IsUnique()
                    .HasDatabaseName("ix_genres_name");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("name");
            });

            // Movie configuration
            modelBuilder.Entity<Movie>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("movies");

                entity.HasIndex(e => e.ImdbId)
                    .IsUnique()
                    .HasDatabaseName("ix_movies_imdb_id");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnName("title");

                entity.Property(e => e.Description)
                    .HasColumnName("description");

                entity.Property(e => e.ReleaseYear)
                    .HasColumnName("release_year");

                entity.Property(e => e.PosterUrl)
                    .HasMaxLength(1000)
                    .HasColumnName("poster_url");

                entity.Property(e => e.ImdbId)
                    .HasMaxLength(20)
                    .HasColumnName("imdb_id");

                entity.Property(e => e.ImdbRating)
                    .HasColumnName("imdb_rating");

                entity.Property(e => e.Runtime)
                    .HasColumnName("runtime");
            });

            // MovieGenre (Many-to-Many join table)
            modelBuilder.Entity<MovieGenre>(entity =>
            {
                entity.HasKey(mg => new { mg.MovieId, mg.GenreId });
                entity.ToTable("movie_genres");

                entity.HasOne(mg => mg.Movie)
                    .WithMany(m => m.MovieGenres)
                    .HasForeignKey(mg => mg.MovieId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(mg => mg.Genre)
                    .WithMany(g => g.MovieGenres)
                    .HasForeignKey(mg => mg.GenreId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Rating configuration
            modelBuilder.Entity<Rating>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("ratings");

                entity.HasIndex(e => new { e.UserId, e.MovieId })
                    .IsUnique()
                    .HasDatabaseName("ix_ratings_user_movie");

                entity.Property(e => e.Score)
                    .IsRequired()
                    .HasColumnName("score");

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Ratings)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Movie)
                    .WithMany(m => m.Ratings)
                    .HasForeignKey(e => e.MovieId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Wishlist configuration
            modelBuilder.Entity<Wishlist>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("wishlists");

                entity.HasIndex(e => new { e.UserId, e.MovieId })
                    .IsUnique()
                    .HasDatabaseName("ix_wishlists_user_movie");

                entity.Property(e => e.AddedAt)
                    .HasColumnName("added_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.WishlistItems)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Movie)
                    .WithMany(m => m.WishlistItems)
                    .HasForeignKey(e => e.MovieId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Comment configuration
            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("comments");

                entity.Property(e => e.Text)
                    .IsRequired()
                    .HasColumnName("text");

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.IsApproved)
                    .HasColumnName("is_approved")
                    .HasDefaultValue(false);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Comments)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Movie)
                    .WithMany(m => m.Comments)
                    .HasForeignKey(e => e.MovieId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // RefreshToken configuration
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("refresh_tokens");

                entity.HasIndex(e => e.Token)
                    .IsUnique()
                    .HasDatabaseName("ix_refresh_tokens_token");

                entity.Property(e => e.Token)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnName("token");

                entity.Property(e => e.ExpiresAt)
                    .HasColumnName("expires_at");

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.IsRevoked)
                    .HasColumnName("is_revoked")
                    .HasDefaultValue(false);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // HiddenRecommendation configuration
            modelBuilder.Entity<HiddenRecommendation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("hidden_recommendations");

                entity.HasIndex(e => new { e.UserId, e.MovieId })
                    .IsUnique()
                    .HasDatabaseName("ix_hidden_recommendations_user_movie");

                entity.Property(e => e.HiddenAt)
                    .HasColumnName("hidden_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Movie)
                    .WithMany()
                    .HasForeignKey(e => e.MovieId)
                    .OnDelete(DeleteBehavior.Cascade);
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