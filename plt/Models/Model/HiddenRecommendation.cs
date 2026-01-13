namespace movieRecom.Models.Model
{
    public class HiddenRecommendation
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int MovieId { get; set; }
        public DateTime HiddenAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
        public Movie Movie { get; set; } = null!;
    }
}
