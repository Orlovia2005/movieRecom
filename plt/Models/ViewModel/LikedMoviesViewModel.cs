using movieRecom.Models.Model;

namespace movieRecom.Models.ViewModel
{
    public class LikedMoviesViewModel
    {
        public List<(Movie Movie, int UserScore)> RatedMovies { get; set; } = new();
        public List<Genre> Genres { get; set; } = new();
        public List<int> SelectedGenreIds { get; set; } = new();
        public int? YearFrom { get; set; }
        public int? YearTo { get; set; }
        public int MinYear { get; set; } = 1990;
        public int MaxYear { get; set; } = DateTime.Now.Year;
        public string? SearchQuery { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
    }
}
