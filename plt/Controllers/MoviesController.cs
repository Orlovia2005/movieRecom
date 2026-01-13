using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using movieRecom.Models.Model;
using movieRecom.Models.ViewModel;

namespace movieRecom.Controllers
{
    public class MoviesController : BaseController
    {
        public MoviesController(EducationDbContext context) : base(context) { }

        // GET: Movies
        public async Task<IActionResult> Index(
            string? search, 
            [FromQuery(Name = "genreIds")] int[]? genreIds, 
            int? genreId,  // Legacy single genre support
            string? genre, // Support genre name from Selection
            int? yearFrom, 
            int? yearTo,
            int? year,     // Legacy single year support
            int page = 1)
        {
            const int pageSize = 12;
            
            var query = _context.Movies
                .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
                .AsQueryable();

            // Поиск по названию
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(m => m.Title.ToLower().Contains(search.ToLower()));
            }

            // Convert genre name to IDs if provided
            var selectedGenreIds = genreIds?.ToList() ?? new List<int>();
            
            // Support legacy single genreId
            if (genreId.HasValue && !selectedGenreIds.Contains(genreId.Value))
            {
                selectedGenreIds.Add(genreId.Value);
            }
            
            // Support genre name from Selection
            if (!string.IsNullOrEmpty(genre))
            {
                var genreByName = await _context.Genres.FirstOrDefaultAsync(g => g.Name == genre);
                if (genreByName != null && !selectedGenreIds.Contains(genreByName.Id))
                {
                    selectedGenreIds.Add(genreByName.Id);
                }
            }

            // Фильтр по жанрам (multiple)
            if (selectedGenreIds.Any())
            {
                query = query.Where(m => m.MovieGenres.Any(mg => selectedGenreIds.Contains(mg.GenreId)));
            }

            // Get min/max years from database
            var minYear = await _context.Movies.Where(m => m.ReleaseYear.HasValue).MinAsync(m => m.ReleaseYear) ?? 1990;
            var maxYear = await _context.Movies.Where(m => m.ReleaseYear.HasValue).MaxAsync(m => m.ReleaseYear) ?? DateTime.Now.Year;

            // Фильтр по диапазону годов
            var effectiveYearFrom = yearFrom ?? year; // Support legacy single year
            if (effectiveYearFrom.HasValue)
            {
                query = query.Where(m => m.ReleaseYear >= effectiveYearFrom.Value);
            }
            if (yearTo.HasValue)
            {
                query = query.Where(m => m.ReleaseYear <= yearTo.Value);
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var movies = await query
                .OrderByDescending(m => m.ImdbRating)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var genres = await _context.Genres.OrderBy(g => g.Name).ToListAsync();
            var years = await _context.Movies
                .Where(m => m.ReleaseYear.HasValue)
                .Select(m => m.ReleaseYear!.Value)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            var viewModel = new MovieCatalogViewModel
            {
                Movies = movies,
                Genres = genres,
                Years = years,
                SelectedGenreIds = selectedGenreIds,
                SelectedGenreId = selectedGenreIds.FirstOrDefault(),
                YearFrom = effectiveYearFrom,
                YearTo = yearTo,
                SelectedYear = effectiveYearFrom,
                MinYear = minYear,
                MaxYear = maxYear,
                SearchQuery = search,
                CurrentPage = page,
                TotalPages = totalPages
            };

            return View(viewModel);
        }

        // GET: Movies/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var movie = await _context.Movies
                .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
                .Include(m => m.Ratings)
                .Include(m => m.Comments)
                .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null)
            {
                return NotFound();
            }

            var user = await GetCurrentUserAsync();
            var userRating = user != null 
                ? await _context.Ratings.FirstOrDefaultAsync(r => r.MovieId == id && r.UserId == user.Id)
                : null;
            var isInWishlist = user != null 
                ? await _context.Wishlists.AnyAsync(w => w.MovieId == id && w.UserId == user.Id)
                : false;

            var viewModel = new MovieDetailsViewModel
            {
                Movie = movie,
                AverageRating = movie.Ratings.Any() ? movie.Ratings.Average(r => r.Score) : 0,
                RatingsCount = movie.Ratings.Count,
                UserRating = userRating?.Score,
                IsInWishlist = isInWishlist,
                Comments = movie.Comments.OrderByDescending(c => c.CreatedAt).ToList()
            };

            return View(viewModel);
        }

        // POST: Movies/Rate
        [HttpPost]
        public async Task<IActionResult> Rate(int movieId, int score)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (score < 1 || score > 5)
            {
                Notif_Error("Оценка должна быть от 1 до 5");
                return RedirectToAction("Details", new { id = movieId });
            }

            var existingRating = await _context.Ratings
                .FirstOrDefaultAsync(r => r.MovieId == movieId && r.UserId == user.Id);

            if (existingRating != null)
            {
                existingRating.Score = score;
                existingRating.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                var rating = new Rating
                {
                    MovieId = movieId,
                    UserId = user.Id,
                    Score = score
                };
                _context.Ratings.Add(rating);
            }

            await _context.SaveChangesAsync();
            Notif_Success("Оценка сохранена!");
            return RedirectToAction("Details", new { id = movieId });
        }

        // POST: Movies/AddComment
        [HttpPost]
        public async Task<IActionResult> AddComment(int movieId, string text)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                Notif_Error("Комментарий не может быть пустым");
                return RedirectToAction("Details", new { id = movieId });
            }

            var comment = new Comment
            {
                MovieId = movieId,
                UserId = user.Id,
                Text = text.Trim(),
                IsApproved = true // Автоодобрение
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            Notif_Info("Комментарий отправлен на модерацию");
            return RedirectToAction("Details", new { id = movieId });
        }

        // API для автодополнения поиска
        [HttpGet]
        public async Task<IActionResult> Search(string term)
        {
            if (string.IsNullOrEmpty(term) || term.Length < 2)
            {
                return Json(new List<object>());
            }

            var movies = await _context.Movies
                .Where(m => m.Title.ToLower().Contains(term.ToLower()))
                .OrderByDescending(m => m.ImdbRating)
                .Take(10)
                .Select(m => new { m.Id, m.Title, m.ReleaseYear, m.PosterUrl })
                .ToListAsync();

            return Json(movies);
        }
    }
}
