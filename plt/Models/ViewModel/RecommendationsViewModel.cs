using movieRecom.Models.Model;

namespace movieRecom.Models.ViewModel
{
    public class RecommendationsViewModel
    {
        public List<MovieRecommendation> Recommendations { get; set; } = new List<MovieRecommendation>();
    }

    public class MovieRecommendation
    {
        public Movie Movie { get; set; } = null!;
        public double Score { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
