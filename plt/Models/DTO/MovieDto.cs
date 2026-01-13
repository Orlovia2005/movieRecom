namespace movieRecom.Models.DTO
{
    public class MovieDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ReleaseYear { get; set; }
        public string? PosterUrl { get; set; }
        public string? ImdbId { get; set; }
        public double? ImdbRating { get; set; }
        public int? Runtime { get; set; }
        public List<string> Genres { get; set; } = new();
        public double? AverageRating { get; set; }
        public int RatingsCount { get; set; }
    }

    public class MovieDetailsDto : MovieDto
    {
        public int? UserRating { get; set; }
        public bool IsInWishlist { get; set; }
        public List<CommentDto> Comments { get; set; } = new();
    }

    public class MovieListDto
    {
        public List<MovieDto> Movies { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class MovieSearchDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int? ReleaseYear { get; set; }
        public string? PosterUrl { get; set; }
    }

    public class CreateMovieDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ReleaseYear { get; set; }
        public string? PosterUrl { get; set; }
        public string? ImdbId { get; set; }
        public double? ImdbRating { get; set; }
        public int? Runtime { get; set; }
        public List<int> GenreIds { get; set; } = new();
    }

    public class UpdateMovieDto : CreateMovieDto
    {
    }
}
