using System.ComponentModel.DataAnnotations;

namespace movieRecom.Models.DTO
{
    public class RatingDto
    {
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public int Score { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateRatingDto
    {
        [Required]
        public int MovieId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Оценка должна быть от 1 до 5")]
        public int Score { get; set; }
    }

    public class CommentDto
    {
        public int Id { get; set; }
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? UserAvatarUrl { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsApproved { get; set; }
    }

    public class CreateCommentDto
    {
        [Required]
        public int MovieId { get; set; }

        [Required(ErrorMessage = "Текст комментария обязателен")]
        [MinLength(1)]
        public string Text { get; set; } = string.Empty;
    }
}
