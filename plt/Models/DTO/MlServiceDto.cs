using System.Text.Json.Serialization;

namespace movieRecom.Models.DTO
{
    /// <summary>
    /// Ответ ML сервиса на запрос рекомендаций
    /// </summary>
    public class MlRecommendationsResponse
    {
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("recommendations")]
        public List<MlRecommendation> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// Отдельная рекомендация от ML сервиса
    /// </summary>
    public class MlRecommendation
    {
        [JsonPropertyName("movie_id")]
        public int MovieId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("predicted_rating")]
        public double PredictedRating { get; set; }

        [JsonPropertyName("genres")]
        public string Genres { get; set; } = string.Empty;

        [JsonPropertyName("imdb_rating")]
        public double? ImdbRating { get; set; }

        [JsonPropertyName("poster_url")]
        public string? PosterUrl { get; set; }

        [JsonPropertyName("release_year")]
        public int? ReleaseYear { get; set; }

        [JsonPropertyName("explanation")]
        public string? Explanation { get; set; }
    }

    /// <summary>
    /// Ответ ML сервиса на запрос похожих фильмов
    /// </summary>
    public class MlSimilarMoviesResponse
    {
        [JsonPropertyName("movie_id")]
        public int MovieId { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("similar_movies")]
        public List<MlSimilarMovie> SimilarMovies { get; set; } = new();
    }

    /// <summary>
    /// Похожий фильм от ML сервиса
    /// </summary>
    public class MlSimilarMovie
    {
        [JsonPropertyName("movie_id")]
        public int MovieId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("similarity_score")]
        public double SimilarityScore { get; set; }

        [JsonPropertyName("genres")]
        public string Genres { get; set; } = string.Empty;

        [JsonPropertyName("poster_url")]
        public string? PosterUrl { get; set; }
    }

    /// <summary>
    /// Ответ health check от ML сервиса
    /// </summary>
    public class MlHealthResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("model_loaded")]
        public bool ModelLoaded { get; set; }

        [JsonPropertyName("service")]
        public string Service { get; set; } = string.Empty;
    }

    /// <summary>
    /// Ответ об ошибке от ML сервиса
    /// </summary>
    public class MlErrorResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
