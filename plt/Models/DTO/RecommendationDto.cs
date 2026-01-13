namespace movieRecom.Models.DTO
{
    public class RecommendationDto
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? PosterUrl { get; set; }
        public int? ReleaseYear { get; set; }
        public double? ImdbRating { get; set; }
        public List<string> Genres { get; set; } = new();
        public string Reason { get; set; } = string.Empty;
        public double Score { get; set; }
    }

    public class RecommendationsResponseDto
    {
        public List<RecommendationDto> Recommendations { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class SimilarMovieDto
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? PosterUrl { get; set; }
        public double Similarity { get; set; }
    }

    public class HideRecommendationDto
    {
        public int MovieId { get; set; }
    }
}
