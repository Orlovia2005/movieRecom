namespace movieRecom.Models.Model
{
    public class Genre
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Navigation - Many-to-Many with Movies
        public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
    }
}
