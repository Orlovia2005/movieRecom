using movieRecom.Models.DTO;

namespace movieRecom.Services
{
    /// <summary>
    /// Интерфейс сервиса для взаимодействия с ML микросервисом рекомендаций
    /// </summary>
    public interface IMlRecommendationService
    {
        /// <summary>
        /// Получить персональные рекомендации для пользователя
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <param name="count">Количество рекомендаций (по умолчанию 10)</param>
        /// <returns>Список рекомендаций или null при ошибке</returns>
        Task<List<MlRecommendation>?> GetRecommendationsAsync(int userId, int count = 10);

        /// <summary>
        /// Получить похожие фильмы на основе контента
        /// </summary>
        /// <param name="movieId">ID фильма</param>
        /// <param name="count">Количество похожих фильмов (по умолчанию 10)</param>
        /// <returns>Список похожих фильмов или null при ошибке</returns>
        Task<List<MlSimilarMovie>?> GetSimilarMoviesAsync(int movieId, int count = 10);

        /// <summary>
        /// Проверить доступность ML сервиса
        /// </summary>
        /// <returns>true если сервис доступен и модель загружена</returns>
        Task<bool> IsHealthyAsync();

        /// <summary>
        /// Проверить статус ML сервиса (детальная информация)
        /// </summary>
        /// <returns>Информация о здоровье сервиса или null при недоступности</returns>
        Task<MlHealthResponse?> GetHealthStatusAsync();
    }
}
