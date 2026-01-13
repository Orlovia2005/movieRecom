namespace movieRecom.Models.Model
{
    public class Rating
    {
        public int Id { get; set; }
        
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int MovieId { get; set; }
        public Movie Movie { get; set; } = null!;

        /// <summary>
        /// Оценка от 1 до 5
        /// </summary>
        public int Score { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
