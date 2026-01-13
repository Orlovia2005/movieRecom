namespace movieRecom.Models.Model
{
    public class Movie
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ReleaseYear { get; set; }
        public string? PosterUrl { get; set; }
        
        // IMDB data
        public string? ImdbId { get; set; }
        public double? ImdbRating { get; set; }
        public int? Runtime { get; set; } // в минутах

        // Navigation properties
        public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
        public ICollection<Wishlist> WishlistItems { get; set; } = new List<Wishlist>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
