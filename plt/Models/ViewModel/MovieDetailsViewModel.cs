using movieRecom.Models.Model;

namespace movieRecom.Models.ViewModel
{
    public class MovieDetailsViewModel
    {
        public Movie Movie { get; set; } = null!;
        public double AverageRating { get; set; }
        public int RatingsCount { get; set; }
        public int? UserRating { get; set; }
        public bool IsInWishlist { get; set; }
        public List<Comment> Comments { get; set; } = new List<Comment>();
    }
}
