using Microsoft.Extensions.Logging;
using movieRecom.Models.DTO;
using movieRecom.Services;
using System.Net;
using System.Text.Json;

namespace movieRecom.Tests.Unit.Services;

public class MlRecommendationServiceTests
{
    private readonly Mock<ILogger<MlRecommendationService>> _loggerMock;

    public MlRecommendationServiceTests()
    {
        _loggerMock = new Mock<ILogger<MlRecommendationService>>();
    }

    private MlRecommendationService CreateService(HttpClient httpClient)
    {
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(f => f.CreateClient("MlService"))
            .Returns(httpClient);

        return new MlRecommendationService(httpClientFactoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetRecommendationsAsync_SuccessfulResponse_ReturnsRecommendations()
    {
        // Arrange
        var expectedResponse = new MlRecommendationsResponse
        {
            UserId = 1,
            Count = 2,
            Recommendations = new List<MlRecommendation>
            {
                new() { MovieId = 1, Title = "Movie 1", PredictedRating = 4.5, Genres = "Action", Explanation = "Test reason" },
                new() { MovieId = 2, Title = "Movie 2", PredictedRating = 4.2, Genres = "Drama", Explanation = "Test reason 2" }
            }
        };

        var messageHandler = new MockHttpMessageHandler(
            JsonSerializer.Serialize(expectedResponse),
            HttpStatusCode.OK);

        var httpClient = new HttpClient(messageHandler)
        {
            BaseAddress = new Uri("http://localhost:5001")
        };

        var service = CreateService(httpClient);

        // Act
        var result = await service.GetRecommendationsAsync(1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result![0].MovieId.Should().Be(1);
        result[0].Title.Should().Be("Movie 1");
        result[0].PredictedRating.Should().Be(4.5);
    }

    [Fact]
    public async Task GetRecommendationsAsync_ServiceUnavailable_ReturnsNull()
    {
        // Arrange
        var messageHandler = new MockHttpMessageHandler(
            "{\"error\": \"Service unavailable\"}",
            HttpStatusCode.ServiceUnavailable);

        var httpClient = new HttpClient(messageHandler)
        {
            BaseAddress = new Uri("http://localhost:5001")
        };

        var service = CreateService(httpClient);

        // Act
        var result = await service.GetRecommendationsAsync(1, 10);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRecommendationsAsync_HttpException_ReturnsNull()
    {
        // Arrange
        var messageHandler = new MockHttpMessageHandler(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(messageHandler)
        {
            BaseAddress = new Uri("http://localhost:5001")
        };

        var service = CreateService(httpClient);

        // Act
        var result = await service.GetRecommendationsAsync(1, 10);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSimilarMoviesAsync_SuccessfulResponse_ReturnsSimilarMovies()
    {
        // Arrange
        var expectedResponse = new MlSimilarMoviesResponse
        {
            MovieId = 1,
            Count = 2,
            SimilarMovies = new List<MlSimilarMovie>
            {
                new() { MovieId = 2, Title = "Similar Movie 1", SimilarityScore = 0.85, Genres = "Action" },
                new() { MovieId = 3, Title = "Similar Movie 2", SimilarityScore = 0.72, Genres = "Action" }
            }
        };

        var messageHandler = new MockHttpMessageHandler(
            JsonSerializer.Serialize(expectedResponse),
            HttpStatusCode.OK);

        var httpClient = new HttpClient(messageHandler)
        {
            BaseAddress = new Uri("http://localhost:5001")
        };

        var service = CreateService(httpClient);

        // Act
        var result = await service.GetSimilarMoviesAsync(1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result![0].SimilarityScore.Should().Be(0.85);
    }

    [Fact]
    public async Task IsHealthyAsync_ServiceHealthyAndModelLoaded_ReturnsTrue()
    {
        // Arrange
        var healthResponse = new MlHealthResponse
        {
            Status = "healthy",
            ModelLoaded = true,
            Service = "ml-recommendations"
        };

        var messageHandler = new MockHttpMessageHandler(
            JsonSerializer.Serialize(healthResponse),
            HttpStatusCode.OK);

        var httpClient = new HttpClient(messageHandler)
        {
            BaseAddress = new Uri("http://localhost:5001")
        };

        var service = CreateService(httpClient);

        // Act
        var result = await service.IsHealthyAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsHealthyAsync_ModelNotLoaded_ReturnsFalse()
    {
        // Arrange
        var healthResponse = new MlHealthResponse
        {
            Status = "healthy",
            ModelLoaded = false,
            Service = "ml-recommendations"
        };

        var messageHandler = new MockHttpMessageHandler(
            JsonSerializer.Serialize(healthResponse),
            HttpStatusCode.OK);

        var httpClient = new HttpClient(messageHandler)
        {
            BaseAddress = new Uri("http://localhost:5001")
        };

        var service = CreateService(httpClient);

        // Act
        var result = await service.IsHealthyAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsHealthyAsync_ServiceUnavailable_ReturnsFalse()
    {
        // Arrange
        var messageHandler = new MockHttpMessageHandler(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(messageHandler)
        {
            BaseAddress = new Uri("http://localhost:5001")
        };

        var service = CreateService(httpClient);

        // Act
        var result = await service.IsHealthyAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetHealthStatusAsync_SuccessfulResponse_ReturnsHealthResponse()
    {
        // Arrange
        var healthResponse = new MlHealthResponse
        {
            Status = "healthy",
            ModelLoaded = true,
            Service = "ml-recommendations"
        };

        var messageHandler = new MockHttpMessageHandler(
            JsonSerializer.Serialize(healthResponse),
            HttpStatusCode.OK);

        var httpClient = new HttpClient(messageHandler)
        {
            BaseAddress = new Uri("http://localhost:5001")
        };

        var service = CreateService(httpClient);

        // Act
        var result = await service.GetHealthStatusAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be("healthy");
        result.ModelLoaded.Should().BeTrue();
        result.Service.Should().Be("ml-recommendations");
    }

    [Fact]
    public async Task GetRecommendationsAsync_InvalidJson_ReturnsNull()
    {
        // Arrange
        var messageHandler = new MockHttpMessageHandler(
            "not valid json",
            HttpStatusCode.OK);

        var httpClient = new HttpClient(messageHandler)
        {
            BaseAddress = new Uri("http://localhost:5001")
        };

        var service = CreateService(httpClient);

        // Act
        var result = await service.GetRecommendationsAsync(1, 10);

        // Assert
        result.Should().BeNull();
    }
}

/// <summary>
/// Mock HttpMessageHandler для тестирования HTTP вызовов
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly string? _responseContent;
    private readonly HttpStatusCode _statusCode;
    private readonly Exception? _exception;

    public MockHttpMessageHandler(string responseContent, HttpStatusCode statusCode)
    {
        _responseContent = responseContent;
        _statusCode = statusCode;
    }

    public MockHttpMessageHandler(Exception exception)
    {
        _exception = exception;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_exception != null)
        {
            throw _exception;
        }

        return Task.FromResult(new HttpResponseMessage
        {
            StatusCode = _statusCode,
            Content = new StringContent(_responseContent ?? string.Empty)
        });
    }
}
