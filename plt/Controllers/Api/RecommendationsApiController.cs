using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using movieRecom.Models.DTO;
using movieRecom.Models.Model;
using movieRecom.Services;

namespace movieRecom.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class RecommendationsController : ControllerBase
    {
        private readonly EducationDbContext _context;
        private readonly IMlRecommendationService _mlService;
        private readonly ILogger<RecommendationsController> _logger;

        public RecommendationsController(
            EducationDbContext context,
            IMlRecommendationService mlService,
            ILogger<RecommendationsController> logger)
        {
            _context = context;
            _mlService = mlService;
            _logger = logger;
        }

        /// <summary>
        /// Получить персональные рекомендации (ML сервис с fallback на локальный алгоритм)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<RecommendationsResponseDto>), 200)]
        public async Task<IActionResult> GetRecommendations([FromQuery] int count = 10)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse.Fail("Не авторизован"));

            // Получаем скрытые фильмы
            var hiddenMovieIds = await _context.HiddenRecommendations
                .Where(h => h.UserId == userId.Value)
                .Select(h => h.MovieId)
                .ToHashSetAsync();

            // Пробуем ML сервис
            var mlRecommendations = await _mlService.GetRecommendationsAsync(userId.Value, count + 5);

            if (mlRecommendations != null && mlRecommendations.Count > 0)
            {
                _logger.LogInformation("API: Используем ML рекомендации для пользователя {UserId}", userId);

                var movieIds = mlRecommendations
                    .Where(r => !hiddenMovieIds.Contains(r.MovieId))
                    .Select(r => r.MovieId)
                    .ToList();

                var movies = await _context.Movies
                    .Where(m => movieIds.Contains(m.Id))
                    .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                    .ToDictionaryAsync(m => m.Id);

                var recommendations = mlRecommendations
                    .Where(r => movies.ContainsKey(r.MovieId) && !hiddenMovieIds.Contains(r.MovieId))
                    .Take(count)
                    .Select(r =>
                    {
                        var movie = movies[r.MovieId];
                        return new RecommendationDto
                        {
                            MovieId = movie.Id,
                            Title = movie.Title,
                            Description = movie.Description,
                            PosterUrl = movie.PosterUrl,
                            ReleaseYear = movie.ReleaseYear,
                            ImdbRating = movie.ImdbRating,
                            Genres = movie.MovieGenres.Select(mg => mg.Genre.Name).ToList(),
                            Reason = r.Explanation ?? $"Предсказанный рейтинг: {r.PredictedRating:F1}",
                            Score = r.PredictedRating * 20
                        };
                    })
                    .ToList();

                if (recommendations.Count > 0)
                {
                    return Ok(ApiResponse<RecommendationsResponseDto>.Ok(new RecommendationsResponseDto
                    {
                        Recommendations = recommendations,
                        TotalCount = recommendations.Count
                    }));
                }
            }

            // Fallback на локальный алгоритм
            _logger.LogInformation("API: Используем локальный алгоритм для пользователя {UserId}", userId);
            return await GetLocalRecommendations(userId.Value, count, hiddenMovieIds);
        }

        /// <summary>
        /// Получить похожие фильмы (ML сервис с fallback)
        /// </summary>
        [HttpGet("similar/{movieId}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<List<SimilarMovieDto>>), 200)]
        public async Task<IActionResult> GetSimilarMovies(int movieId, [FromQuery] int count = 10)
        {
            var movie = await _context.Movies
                .Include(m => m.MovieGenres)
                .FirstOrDefaultAsync(m => m.Id == movieId);

            if (movie == null)
                return NotFound(ApiResponse.Fail("Фильм не найден"));

            // Пробуем ML сервис
            var mlSimilar = await _mlService.GetSimilarMoviesAsync(movieId, count + 5);

            if (mlSimilar != null && mlSimilar.Count > 0)
            {
                _logger.LogInformation("API: Используем ML для похожих фильмов на {MovieId}", movieId);

                var similarMovieIds = mlSimilar.Select(s => s.MovieId).ToList();
                var similarMovies = await _context.Movies
                    .Where(m => similarMovieIds.Contains(m.Id))
                    .ToDictionaryAsync(m => m.Id);

                var result = mlSimilar
                    .Where(s => similarMovies.ContainsKey(s.MovieId))
                    .Take(count)
                    .Select(s => new SimilarMovieDto
                    {
                        MovieId = s.MovieId,
                        Title = similarMovies[s.MovieId].Title,
                        PosterUrl = similarMovies[s.MovieId].PosterUrl,
                        Similarity = s.SimilarityScore
                    })
                    .ToList();

                if (result.Count > 0)
                {
                    return Ok(ApiResponse<List<SimilarMovieDto>>.Ok(result));
                }
            }

            // Fallback на локальный алгоритм
            _logger.LogInformation("API: Используем локальный алгоритм для похожих фильмов на {MovieId}", movieId);
            return await GetLocalSimilarMovies(movie, count);
        }

        /// <summary>
        /// Скрыть рекомендацию (больше не показывать этот фильм)
        /// </summary>
        [HttpPost("hide")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        public async Task<IActionResult> HideRecommendation([FromBody] HideRecommendationDto model)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse.Fail("Не авторизован"));

            var exists = await _context.HiddenRecommendations
                .AnyAsync(h => h.UserId == userId.Value && h.MovieId == model.MovieId);

            if (exists)
                return Ok(ApiResponse.Ok("Рекомендация уже скрыта"));

            var hidden = new HiddenRecommendation
            {
                UserId = userId.Value,
                MovieId = model.MovieId,
                HiddenAt = DateTime.UtcNow
            };

            await _context.HiddenRecommendations.AddAsync(hidden);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Ok("Рекомендация скрыта"));
        }

        /// <summary>
        /// Получить скрытые рекомендации
        /// </summary>
        [HttpGet("hidden")]
        [ProducesResponseType(typeof(ApiResponse<List<MovieDto>>), 200)]
        public async Task<IActionResult> GetHiddenRecommendations()
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse.Fail("Не авторизован"));

            var hiddenMovies = await _context.HiddenRecommendations
                .Where(h => h.UserId == userId.Value)
                .Include(h => h.Movie)
                    .ThenInclude(m => m.MovieGenres)
                        .ThenInclude(mg => mg.Genre)
                .Select(h => new MovieDto
                {
                    Id = h.Movie.Id,
                    Title = h.Movie.Title,
                    PosterUrl = h.Movie.PosterUrl,
                    ReleaseYear = h.Movie.ReleaseYear,
                    Genres = h.Movie.MovieGenres.Select(mg => mg.Genre.Name).ToList()
                })
                .ToListAsync();

            return Ok(ApiResponse<List<MovieDto>>.Ok(hiddenMovies));
        }

        /// <summary>
        /// Восстановить скрытую рекомендацию
        /// </summary>
        [HttpDelete("hidden/{movieId}")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        public async Task<IActionResult> UnhideRecommendation(int movieId)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse.Fail("Не авторизован"));

            var hidden = await _context.HiddenRecommendations
                .FirstOrDefaultAsync(h => h.UserId == userId.Value && h.MovieId == movieId);

            if (hidden == null)
                return NotFound(ApiResponse.Fail("Рекомендация не найдена в скрытых"));

            _context.HiddenRecommendations.Remove(hidden);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Ok("Рекомендация восстановлена"));
        }

        /// <summary>
        /// Проверить статус ML сервиса
        /// </summary>
        [HttpGet("ml-status")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<MlHealthResponse>), 200)]
        public async Task<IActionResult> GetMlStatus()
        {
            var health = await _mlService.GetHealthStatusAsync();
            if (health == null)
            {
                return Ok(ApiResponse<object>.Ok(new
                {
                    Status = "unavailable",
                    ModelLoaded = false,
                    Message = "ML сервис недоступен"
                }));
            }
            return Ok(ApiResponse<MlHealthResponse>.Ok(health));
        }

        #region Private Methods

        private async Task<IActionResult> GetLocalRecommendations(int userId, int count, HashSet<int> hiddenMovieIds)
        {
            var ratedMovieIds = await _context.Ratings
                .Where(r => r.UserId == userId)
                .Select(r => r.MovieId)
                .ToListAsync();

            var wishlistMovieIds = await _context.Wishlists
                .Where(w => w.UserId == userId)
                .Select(w => w.MovieId)
                .ToListAsync();

            var excludeIds = ratedMovieIds.Union(wishlistMovieIds).Union(hiddenMovieIds).ToHashSet();

            var topGenres = await _context.Ratings
                .Where(r => r.UserId == userId && r.Score >= 4)
                .SelectMany(r => r.Movie.MovieGenres)
                .GroupBy(mg => mg.Genre.Name)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToListAsync();

            var movies = await _context.Movies
                .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                .Where(m => !excludeIds.Contains(m.Id))
                .OrderByDescending(m => m.ImdbRating)
                .Take(count * 3)
                .ToListAsync();

            var recommendations = movies
                .Select(m =>
                {
                    var movieGenres = m.MovieGenres.Select(mg => mg.Genre.Name).ToList();
                    var genreMatches = movieGenres.Intersect(topGenres).Count();
                    var score = (genreMatches * 20) + ((m.ImdbRating ?? 0) * 5);

                    return new RecommendationDto
                    {
                        MovieId = m.Id,
                        Title = m.Title,
                        Description = m.Description,
                        PosterUrl = m.PosterUrl,
                        ReleaseYear = m.ReleaseYear,
                        ImdbRating = m.ImdbRating,
                        Genres = movieGenres,
                        Reason = GenerateReason(movieGenres, topGenres, m.ImdbRating),
                        Score = score
                    };
                })
                .OrderByDescending(r => r.Score)
                .Take(count)
                .ToList();

            return Ok(ApiResponse<RecommendationsResponseDto>.Ok(new RecommendationsResponseDto
            {
                Recommendations = recommendations,
                TotalCount = recommendations.Count
            }));
        }

        private async Task<IActionResult> GetLocalSimilarMovies(Movie movie, int count)
        {
            var movieGenreIds = movie.MovieGenres.Select(mg => mg.GenreId).ToList();

            var similarMovies = await _context.Movies
                .Include(m => m.MovieGenres)
                .Where(m => m.Id != movie.Id)
                .Where(m => m.MovieGenres.Any(mg => movieGenreIds.Contains(mg.GenreId)))
                .OrderByDescending(m => m.MovieGenres.Count(mg => movieGenreIds.Contains(mg.GenreId)))
                .ThenByDescending(m => m.ImdbRating)
                .Take(count)
                .Select(m => new SimilarMovieDto
                {
                    MovieId = m.Id,
                    Title = m.Title,
                    PosterUrl = m.PosterUrl,
                    Similarity = movieGenreIds.Count > 0
                        ? (double)m.MovieGenres.Count(mg => movieGenreIds.Contains(mg.GenreId)) / movieGenreIds.Count
                        : 0
                })
                .ToListAsync();

            return Ok(ApiResponse<List<SimilarMovieDto>>.Ok(similarMovies));
        }

        private string GenerateReason(List<string> movieGenres, List<string> userGenres, double? imdbRating)
        {
            var matchingGenres = movieGenres.Intersect(userGenres).ToList();

            if (matchingGenres.Any())
            {
                var genresText = string.Join(", ", matchingGenres.Take(2));
                if (imdbRating >= 7.0)
                    return $"Высокий рейтинг ({imdbRating:F1}) и жанры: {genresText}";
                return $"Похоже на фильмы, которые вам нравятся ({genresText})";
            }

            if (imdbRating >= 7.5)
                return $"Высоко оценённый фильм (IMDB: {imdbRating:F1})";

            return "Рекомендуем попробовать что-то новое";
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out int userId))
                return userId;
            return null;
        }

        #endregion
    }
}
