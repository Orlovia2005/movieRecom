using Microsoft.EntityFrameworkCore;
using movieRecom.Models.Model;

namespace movieRecom.Tests.Helpers;

/// <summary>
/// Фабрика для создания in-memory DbContext для тестов
/// </summary>
public static class TestDbContextFactory
{
    public static EducationDbContext CreateInMemoryContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<EducationDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        var context = new EducationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static async Task<EducationDbContext> CreateSeededContextAsync(string? databaseName = null)
    {
        var context = CreateInMemoryContext(databaseName);
        await SeedTestDataAsync(context);
        return context;
    }

    private static async Task SeedTestDataAsync(EducationDbContext context)
    {
        // Жанры
        var genres = new List<Genre>
        {
            new() { Id = 1, Name = "Action" },
            new() { Id = 2, Name = "Drama" },
            new() { Id = 3, Name = "Comedy" },
            new() { Id = 4, Name = "Sci-Fi" },
            new() { Id = 5, Name = "Horror" }
        };
        await context.Genres.AddRangeAsync(genres);

        // Пользователи
        var users = new List<User>
        {
            new()
            {
                Id = 1,
                Name = "TestUser",
                LastName = "One",
                Email = "user1@test.com",
                Password = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = UserRole.User
            },
            new()
            {
                Id = 2,
                Name = "AdminUser",
                LastName = "Admin",
                Email = "admin@test.com",
                Password = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = UserRole.Admin
            }
        };
        await context.Users.AddRangeAsync(users);

        // Фильмы
        var movies = new List<Movie>
        {
            new()
            {
                Id = 1,
                Title = "Test Movie 1",
                Description = "Action movie description",
                ReleaseYear = 2023,
                ImdbRating = 8.5,
                PosterUrl = "https://example.com/poster1.jpg"
            },
            new()
            {
                Id = 2,
                Title = "Test Movie 2",
                Description = "Drama movie description",
                ReleaseYear = 2022,
                ImdbRating = 7.8,
                PosterUrl = "https://example.com/poster2.jpg"
            },
            new()
            {
                Id = 3,
                Title = "Test Movie 3",
                Description = "Comedy movie description",
                ReleaseYear = 2024,
                ImdbRating = 6.5,
                PosterUrl = "https://example.com/poster3.jpg"
            }
        };
        await context.Movies.AddRangeAsync(movies);

        // Связи фильм-жанр
        var movieGenres = new List<MovieGenre>
        {
            new() { MovieId = 1, GenreId = 1 }, // Movie 1 - Action
            new() { MovieId = 1, GenreId = 4 }, // Movie 1 - Sci-Fi
            new() { MovieId = 2, GenreId = 2 }, // Movie 2 - Drama
            new() { MovieId = 3, GenreId = 3 }  // Movie 3 - Comedy
        };
        await context.MovieGenres.AddRangeAsync(movieGenres);

        // Оценки
        var ratings = new List<Rating>
        {
            new() { Id = 1, UserId = 1, MovieId = 1, Score = 5, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, UserId = 1, MovieId = 2, Score = 4, CreatedAt = DateTime.UtcNow }
        };
        await context.Ratings.AddRangeAsync(ratings);

        // Wishlist
        var wishlists = new List<Wishlist>
        {
            new() { Id = 1, UserId = 1, MovieId = 3, AddedAt = DateTime.UtcNow }
        };
        await context.Wishlists.AddRangeAsync(wishlists);

        await context.SaveChangesAsync();
    }
}
