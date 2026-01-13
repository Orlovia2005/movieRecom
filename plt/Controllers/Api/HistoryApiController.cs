using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using movieRecom.Models.DTO;
using movieRecom.Models.Model;

namespace movieRecom.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class HistoryController : ControllerBase
    {
        private readonly EducationDbContext _context;

        public HistoryController(EducationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получить полную историю активности пользователя
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<UserHistoryDto>), 200)]
        public async Task<IActionResult> GetHistory()
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse.Fail("Не авторизован"));

            var ratings = await _context.Ratings
                .Where(r => r.UserId == userId.Value)
                .Include(r => r.Movie)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new HistoryItemDto
                {
                    Type = "rating",
                    MovieId = r.MovieId,
                    MovieTitle = r.Movie.Title,
                    MoviePosterUrl = r.Movie.PosterUrl,
                    CreatedAt = r.CreatedAt,
                    Details = $"Оценка: {r.Score}/5"
                })
                .ToListAsync();

            var wishlistItems = await _context.Wishlists
                .Where(w => w.UserId == userId.Value)
                .Include(w => w.Movie)
                .OrderByDescending(w => w.AddedAt)
                .Select(w => new HistoryItemDto
                {
                    Type = "wishlist",
                    MovieId = w.MovieId,
                    MovieTitle = w.Movie.Title,
                    MoviePosterUrl = w.Movie.PosterUrl,
                    CreatedAt = w.AddedAt,
                    Details = "Добавлено в избранное"
                })
                .ToListAsync();

            var comments = await _context.Comments
                .Where(c => c.UserId == userId.Value)
                .Include(c => c.Movie)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new HistoryItemDto
                {
                    Type = "comment",
                    MovieId = c.MovieId,
                    MovieTitle = c.Movie.Title,
                    MoviePosterUrl = c.Movie.PosterUrl,
                    CreatedAt = c.CreatedAt,
                    Details = c.IsApproved ? "Комментарий опубликован" : "Комментарий на модерации"
                })
                .ToListAsync();

            // Объединяем и сортируем по дате
            var allHistory = ratings
                .Concat(wishlistItems)
                .Concat(comments)
                .OrderByDescending(h => h.CreatedAt)
                .ToList();

            var result = new UserHistoryDto
            {
                Items = allHistory,
                TotalRatings = ratings.Count,
                TotalWishlist = wishlistItems.Count,
                TotalComments = comments.Count
            };

            return Ok(ApiResponse<UserHistoryDto>.Ok(result));
        }

        /// <summary>
        /// Получить историю оценок
        /// </summary>
        [HttpGet("ratings")]
        [ProducesResponseType(typeof(ApiResponse<List<RatingHistoryDto>>), 200)]
        public async Task<IActionResult> GetRatingsHistory()
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse.Fail("Не авторизован"));

            var ratings = await _context.Ratings
                .Where(r => r.UserId == userId.Value)
                .Include(r => r.Movie)
                    .ThenInclude(m => m.MovieGenres)
                        .ThenInclude(mg => mg.Genre)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new RatingHistoryDto
                {
                    MovieId = r.MovieId,
                    MovieTitle = r.Movie.Title,
                    MoviePosterUrl = r.Movie.PosterUrl,
                    Genres = r.Movie.MovieGenres.Select(mg => mg.Genre.Name).ToList(),
                    Score = r.Score,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<List<RatingHistoryDto>>.Ok(ratings));
        }

        /// <summary>
        /// Получить историю комментариев
        /// </summary>
        [HttpGet("comments")]
        [ProducesResponseType(typeof(ApiResponse<List<CommentHistoryDto>>), 200)]
        public async Task<IActionResult> GetCommentsHistory()
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse.Fail("Не авторизован"));

            var comments = await _context.Comments
                .Where(c => c.UserId == userId.Value)
                .Include(c => c.Movie)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentHistoryDto
                {
                    Id = c.Id,
                    MovieId = c.MovieId,
                    MovieTitle = c.Movie.Title,
                    MoviePosterUrl = c.Movie.PosterUrl,
                    Text = c.Text,
                    IsApproved = c.IsApproved,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<List<CommentHistoryDto>>.Ok(comments));
        }

        /// <summary>
        /// Получить статистику пользователя
        /// </summary>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(ApiResponse<UserStatsDto>), 200)]
        public async Task<IActionResult> GetUserStats()
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse.Fail("Не авторизован"));

            var ratings = await _context.Ratings
                .Where(r => r.UserId == userId.Value)
                .ToListAsync();

            var topGenres = await _context.Ratings
                .Where(r => r.UserId == userId.Value && r.Score >= 4)
                .SelectMany(r => r.Movie.MovieGenres)
                .GroupBy(mg => mg.Genre.Name)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => new GenreStatDto
                {
                    Genre = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var stats = new UserStatsDto
            {
                TotalRatings = ratings.Count,
                AverageRating = ratings.Any() ? Math.Round(ratings.Average(r => r.Score), 1) : 0,
                TotalWishlist = await _context.Wishlists.CountAsync(w => w.UserId == userId.Value),
                TotalComments = await _context.Comments.CountAsync(c => c.UserId == userId.Value),
                TopGenres = topGenres,
                RatingDistribution = new RatingDistributionDto
                {
                    One = ratings.Count(r => r.Score == 1),
                    Two = ratings.Count(r => r.Score == 2),
                    Three = ratings.Count(r => r.Score == 3),
                    Four = ratings.Count(r => r.Score == 4),
                    Five = ratings.Count(r => r.Score == 5)
                }
            };

            return Ok(ApiResponse<UserStatsDto>.Ok(stats));
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out int userId))
                return userId;
            return null;
        }
    }

    // DTO классы для History
    public class UserHistoryDto
    {
        public List<HistoryItemDto> Items { get; set; } = new();
        public int TotalRatings { get; set; }
        public int TotalWishlist { get; set; }
        public int TotalComments { get; set; }
    }

    public class HistoryItemDto
    {
        public string Type { get; set; } = string.Empty; // rating, wishlist, comment
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string? MoviePosterUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Details { get; set; } = string.Empty;
    }

    public class RatingHistoryDto
    {
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string? MoviePosterUrl { get; set; }
        public List<string> Genres { get; set; } = new();
        public int Score { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CommentHistoryDto
    {
        public int Id { get; set; }
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string? MoviePosterUrl { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserStatsDto
    {
        public int TotalRatings { get; set; }
        public double AverageRating { get; set; }
        public int TotalWishlist { get; set; }
        public int TotalComments { get; set; }
        public List<GenreStatDto> TopGenres { get; set; } = new();
        public RatingDistributionDto RatingDistribution { get; set; } = new();
    }

    public class GenreStatDto
    {
        public string Genre { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class RatingDistributionDto
    {
        public int One { get; set; }
        public int Two { get; set; }
        public int Three { get; set; }
        public int Four { get; set; }
        public int Five { get; set; }
    }
}
