using System.Text.Json;
using movieRecom.Models.DTO;

namespace movieRecom.Services
{
    /// <summary>
    /// Сервис для взаимодействия с ML микросервисом рекомендаций
    /// </summary>
    public class MlRecommendationService : IMlRecommendationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MlRecommendationService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public MlRecommendationService(
            IHttpClientFactory httpClientFactory,
            ILogger<MlRecommendationService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<MlRecommendation>?> GetRecommendationsAsync(int userId, int count = 10)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("MlService");
                var url = $"/recommendations/{userId}?n={count}&explain=true";

                _logger.LogInformation("Запрос рекомендаций для пользователя {UserId}, count={Count}", userId, count);

                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning(
                        "ML сервис вернул ошибку {StatusCode}: {Error}",
                        (int)response.StatusCode,
                        errorContent);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MlRecommendationsResponse>(content, _jsonOptions);

                _logger.LogInformation(
                    "Получено {Count} рекомендаций для пользователя {UserId}",
                    result?.Recommendations?.Count ?? 0,
                    userId);

                return result?.Recommendations;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Ошибка HTTP при запросе рекомендаций для пользователя {UserId}", userId);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Таймаут при запросе рекомендаций для пользователя {UserId}", userId);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Ошибка десериализации ответа ML сервиса для пользователя {UserId}", userId);
                return null;
            }
        }

        public async Task<List<MlSimilarMovie>?> GetSimilarMoviesAsync(int movieId, int count = 10)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("MlService");
                var url = $"/similar/{movieId}?n={count}";

                _logger.LogInformation("Запрос похожих фильмов для movie {MovieId}, count={Count}", movieId, count);

                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning(
                        "ML сервис вернул ошибку {StatusCode} при запросе похожих фильмов: {Error}",
                        (int)response.StatusCode,
                        errorContent);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MlSimilarMoviesResponse>(content, _jsonOptions);

                _logger.LogInformation(
                    "Получено {Count} похожих фильмов для movie {MovieId}",
                    result?.SimilarMovies?.Count ?? 0,
                    movieId);

                return result?.SimilarMovies;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Ошибка HTTP при запросе похожих фильмов для movie {MovieId}", movieId);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Таймаут при запросе похожих фильмов для movie {MovieId}", movieId);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Ошибка десериализации ответа ML сервиса для movie {MovieId}", movieId);
                return null;
            }
        }

        public async Task<bool> IsHealthyAsync()
        {
            var health = await GetHealthStatusAsync();
            return health != null && health.Status == "healthy" && health.ModelLoaded;
        }

        public async Task<MlHealthResponse?> GetHealthStatusAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("MlService");

                var response = await client.GetAsync("/health");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("ML сервис недоступен, статус: {StatusCode}", (int)response.StatusCode);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MlHealthResponse>(content, _jsonOptions);

                _logger.LogDebug(
                    "ML сервис статус: {Status}, модель загружена: {ModelLoaded}",
                    result?.Status,
                    result?.ModelLoaded);

                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "ML сервис недоступен (HTTP ошибка)");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "ML сервис недоступен (таймаут)");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Неожиданная ошибка при проверке ML сервиса");
                return null;
            }
        }
    }
}
