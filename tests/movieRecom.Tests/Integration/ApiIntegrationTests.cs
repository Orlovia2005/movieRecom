using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using movieRecom.Models.DTO;
using movieRecom.Models.Model;
using movieRecom.Services;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace movieRecom.Tests.Integration;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Удаляем реальный DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<EducationDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Добавляем In-Memory DbContext
                services.AddDbContext<EducationDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));

                // Mock ML Service
                services.AddScoped<IMlRecommendationService, MockMlRecommendationService>();
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMovies_ReturnsSuccessAndMovies()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync("/api/movies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetMovieById_ExistingMovie_ReturnsMovie()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync("/api/movies/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMovieById_NonExistingMovie_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/movies/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RegisterUser_ValidData_ReturnsSuccess()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = $"newuser{Guid.NewGuid()}@test.com",
            Password = "Password123!",
            Name = "New User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("accessToken");
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var email = $"logintest{Guid.NewGuid()}@test.com";
        var password = "Password123!";

        // Регистрируем пользователя
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            Email = email,
            Password = password,
            Name = "Login Test User"
        });

        // Act - пытаемся залогиниться
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = email,
            Password = password
        });

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await loginResponse.Content.ReadAsStringAsync();
        content.Should().Contain("accessToken");
        content.Should().Contain("refreshToken");
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "nonexistent@test.com",
            Password = "wrongpassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetRecommendations_Unauthorized_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/recommendations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetRecommendations_Authorized_ReturnsRecommendations()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/recommendations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MlStatus_ReturnsStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/recommendations/ml-status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SimilarMovies_ReturnsMovies()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync("/api/recommendations/similar/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetGenres_ReturnsGenres()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync("/api/movies/genres");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #region Helper Methods

    private async Task SeedTestDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EducationDbContext>();

        if (!await context.Genres.AnyAsync())
        {
            context.Genres.AddRange(
                new Genre { Id = 1, Name = "Action" },
                new Genre { Id = 2, Name = "Drama" }
            );

            context.Movies.AddRange(
                new Movie
                {
                    Id = 1,
                    Title = "Test Movie",
                    Description = "Test",
                    ReleaseYear = 2023,
                    ImdbRating = 8.0
                }
            );

            context.MovieGenres.Add(new MovieGenre { MovieId = 1, GenreId = 1 });

            await context.SaveChangesAsync();
        }
    }

    private async Task<string> GetAuthTokenAsync()
    {
        var email = $"authtest{Guid.NewGuid()}@test.com";
        var password = "TestPassword123!";

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            Email = email,
            Password = password,
            Name = "Auth Test User"
        });

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = email,
            Password = password
        });

        var content = await loginResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);

        if (doc.RootElement.TryGetProperty("data", out var data) &&
            data.TryGetProperty("accessToken", out var tokenElement))
        {
            return tokenElement.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    #endregion
}

/// <summary>
/// Mock ML service для интеграционных тестов
/// </summary>
public class MockMlRecommendationService : IMlRecommendationService
{
    public Task<List<MlRecommendation>?> GetRecommendationsAsync(int userId, int count = 10)
    {
        var recommendations = new List<MlRecommendation>
        {
            new()
            {
                MovieId = 1,
                Title = "Mock Movie",
                PredictedRating = 4.5,
                Genres = "Action",
                Explanation = "Mock recommendation"
            }
        };
        return Task.FromResult<List<MlRecommendation>?>(recommendations);
    }

    public Task<List<MlSimilarMovie>?> GetSimilarMoviesAsync(int movieId, int count = 10)
    {
        var similar = new List<MlSimilarMovie>
        {
            new()
            {
                MovieId = 2,
                Title = "Similar Mock Movie",
                SimilarityScore = 0.8,
                Genres = "Action"
            }
        };
        return Task.FromResult<List<MlSimilarMovie>?>(similar);
    }

    public Task<bool> IsHealthyAsync() => Task.FromResult(true);

    public Task<MlHealthResponse?> GetHealthStatusAsync()
    {
        return Task.FromResult<MlHealthResponse?>(new MlHealthResponse
        {
            Status = "healthy",
            ModelLoaded = true,
            Service = "mock-ml-service"
        });
    }
}
