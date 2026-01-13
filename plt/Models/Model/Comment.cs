namespace movieRecom.Models.Model
{
    public class Comment
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int MovieId { get; set; }
        public Movie Movie { get; set; } = null!;

        public string Text { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Для модерации отзывов
        /// </summary>
        public bool IsApproved { get; set; } = false;
    }
}
