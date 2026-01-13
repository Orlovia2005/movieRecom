using movieRecom.Models.Model;

namespace movieRecom.Models.ViewModel
{
    public class MovieCatalogViewModel
    {
        public List<Movie> Movies { get; set; } = new List<Movie>();
        public List<Genre> Genres { get; set; } = new List<Genre>();
        public List<int> Years { get; set; } = new List<int>();
        
        // Support multiple genre selection
        public List<int> SelectedGenreIds { get; set; } = new List<int>();
        
        // Year range
        public int? YearFrom { get; set; }
        public int? YearTo { get; set; }
        public int MinYear { get; set; } = 1990;
        public int MaxYear { get; set; } = DateTime.Now.Year;
        
        public string? SearchQuery { get; set; }
        
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        
        // Legacy support for single genre (kept for backward compatibility)
        public int? SelectedGenreId { get; set; }
        public int? SelectedYear { get; set; }
    }
}
