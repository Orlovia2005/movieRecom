using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using movieRecom.Models.Model;
using movieRecom.Models.ViewModel;
using movieRecom.Services;

namespace movieRecom.Controllers
{
    public class RecommendationsController : BaseController
    {
        private readonly IMlRecommendationService _mlService;
        private readonly ILogger<RecommendationsController> _logger;

        public RecommendationsController(
            EducationDbContext context,
            IMlRecommendationService mlService,
            ILogger<RecommendationsController> logger) : base(context)
        {
            _mlService = mlService;
            _logger = logger;
        }

        // GET: Recommendations
        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var recommendations = await GetRecommendationsForUser(user.Id);

            var viewModel = new RecommendationsViewModel
            {
                Recommendations = recommendations
            };

            return View(viewModel);
        }

        // POST: Hide recommendation
        [HttpPost]
        public async Task<IActionResult> Hide(int movieId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var exists = await _context.HiddenRecommendations
                .AnyAsync(h => h.UserId == user.Id && h.MovieId == movieId);

            if (!exists)
            {
                var hidden = new HiddenRecommendation
                {
                    UserId = user.Id,
                    MovieId = movieId,
                    HiddenAt = DateTime.UtcNow
                };
                await _context.HiddenRecommendations.AddAsync(hidden);
                await _context.SaveChangesAsync();
                Notif_Success("Рекомендация скрыта");
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Получение рекомендаций с приоритетом ML сервиса и fallback на локальный алгоритм
        /// </summary>
        private async Task<List<MovieRecommendation>> GetRecommendationsForUser(int userId)
        {
            // Получаем скрытые рекомендации для исключения
            var hiddenMovieIds = await _context.HiddenRecommendations
                .Where(h => h.UserId == userId)
                .Select(h => h.MovieId)
                .ToListAsync();

            // Пробуем получить рекомендации от ML сервиса
            var mlRecommendations = await _mlService.GetRecommendationsAsync(userId, 30);

            if (mlRecommendations != null && mlRecommendations.Count > 0)
            {
                _logger.LogInformation("Используем рекомендации от ML сервиса для пользователя {UserId}", userId);

                // Получаем фильмы из БД по ID из ML рекомендаций
                var movieIds = mlRecommendations.Select(r => r.MovieId).ToList();
                var movies = await _context.Movies
                    .Where(m => movieIds.Contains(m.Id) && !hiddenMovieIds.Contains(m.Id))
                    .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                    .ToDictionaryAsync(m => m.Id);

                var recommendations = mlRecommendations
                    .Where(r => movies.ContainsKey(r.MovieId) && !hiddenMovieIds.Contains(r.MovieId))
                    .Select(r => new MovieRecommendation
                    {
                        Movie = movies[r.MovieId],
                        Score = r.PredictedRating * 20, // Масштабируем для отображения
                        Reason = r.Explanation ?? $"Предсказанный рейтинг: {r.PredictedRating:F1}"
                    })
                    .Take(30)
                    .ToList();

                if (recommendations.Count > 0)
                {
                    return recommendations;
                }
            }

            // Fallback на локальный алгоритм
            _logger.LogInformation("Используем локальный алгоритм рекомендаций для пользователя {UserId}", userId);
            return await GetLocalRecommendationsForUser(userId, hiddenMovieIds);
        }

        /// <summary>
        /// Локальный алгоритм рекомендаций на основе жанровых предпочтений
        /// </summary>
        private async Task<List<MovieRecommendation>> GetLocalRecommendationsForUser(int userId, List<int> hiddenMovieIds)
        {
            // Получаем оценённые фильмы
            var userRatings = await _context.Ratings
                .Where(r => r.UserId == userId)
                .Include(r => r.Movie)
                .ThenInclude(m => m.MovieGenres)
                .ToListAsync();

            // Получаем wishlist
            var wishlistMovieIds = await _context.Wishlists
                .Where(w => w.UserId == userId)
                .Select(w => w.MovieId)
                .ToListAsync();

            // Исключаем уже оценённые, из wishlist и скрытые
            var excludeIds = userRatings.Select(r => r.MovieId)
                .Concat(wishlistMovieIds)
                .Concat(hiddenMovieIds)
                .Distinct()
                .ToList();

            // Анализируем жанровые предпочтения (из высоко оценённых фильмов: 4-5)
            var preferredGenreIds = userRatings
                .Where(r => r.Score >= 4)
                .SelectMany(r => r.Movie.MovieGenres.Select(mg => mg.GenreId))
                .GroupBy(id => id)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToList();

            // Если у пользователя нет оценок, берём популярные жанры
            if (!preferredGenreIds.Any())
            {
                preferredGenreIds = await _context.MovieGenres
                    .GroupBy(mg => mg.GenreId)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => g.Key)
                    .ToListAsync();
            }

            // Получаем рекомендации
            var recommendedMovies = await _context.Movies
                .Where(m => !excludeIds.Contains(m.Id))
                .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
                .ToListAsync();

            // Рассчитываем score для каждого фильма
            var recommendations = recommendedMovies
                .Select(m =>
                {
                    double score = 0;
                    var movieGenreIds = m.MovieGenres.Select(mg => mg.GenreId).ToList();

                    // Бонус за совпадение жанров
                    int genreMatches = movieGenreIds.Intersect(preferredGenreIds).Count();
                    score += genreMatches * 20;

                    // Бонус за IMDB рейтинг
                    if (m.ImdbRating.HasValue)
                    {
                        score += m.ImdbRating.Value * 5;
                    }

                    // Бонус за свежесть (фильмы за последние 5 лет)
                    if (m.ReleaseYear.HasValue && m.ReleaseYear >= DateTime.Now.Year - 5)
                    {
                        score += 10;
                    }

                    string reason = genreMatches > 0
                        ? $"Похоже на фильмы, которые вам нравятся ({string.Join(", ", m.MovieGenres.Where(mg => preferredGenreIds.Contains(mg.GenreId)).Select(mg => mg.Genre.Name))})"
                        : "Популярный фильм с высоким рейтингом";

                    return new MovieRecommendation
                    {
                        Movie = m,
                        Score = score,
                        Reason = reason
                    };
                })
                .OrderByDescending(r => r.Score)
                .Take(30)
                .ToList();

            return recommendations;
        }
    }
}
