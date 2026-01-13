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
    public class MoviesController : ControllerBase
    {
        private readonly EducationDbContext _context;

        public MoviesController(EducationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получить список фильмов с фильтрацией и пагинацией
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<MovieListDto>), 200)]
        public async Task<IActionResult> GetMovies(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12,
            [FromQuery] int? genreId = null,
            [FromQuery] int? year = null,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = "title")
        {
            var query = _context.Movies
                .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                .Include(m => m.Ratings)
                .AsQueryable();

            // Фильтр по жанру
            if (genreId.HasValue)
            {
                query = query.Where(m => m.MovieGenres.Any(mg => mg.GenreId == genreId.Value));
            }

            // Фильтр по году
            if (year.HasValue)
            {
                query = query.Where(m => m.ReleaseYear == year.Value);
            }

            // Поиск по названию
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(m => m.Title.ToLower().Contains(search.ToLower()));
            }

            // Сортировка
            query = sortBy?.ToLower() switch
            {
                "year" => query.OrderByDescending(m => m.ReleaseYear),
                "rating" => query.OrderByDescending(m => m.ImdbRating),
                "title" => query.OrderBy(m => m.Title),
                _ => query.OrderBy(m => m.Title)
            };

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var movies = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MovieDto
                {
                    Id = m.Id,
                    Title = m.Title,
                    Description = m.Description,
                    ReleaseYear = m.ReleaseYear,
                    PosterUrl = m.PosterUrl,
                    ImdbId = m.ImdbId,
                    ImdbRating = m.ImdbRating,
                    Runtime = m.Runtime,
                    Genres = m.MovieGenres.Select(mg => mg.Genre.Name).ToList(),
                    AverageRating = m.Ratings.Any() ? m.Ratings.Average(r => r.Score) : null,
                    RatingsCount = m.Ratings.Count
                })
                .ToListAsync();

            var result = new MovieListDto
            {
                Movies = movies,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            };

            return Ok(ApiResponse<MovieListDto>.Ok(result));
        }

        /// <summary>
        /// Получить фильм по ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<MovieDetailsDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> GetMovie(int id)
        {
            var movie = await _context.Movies
                .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                .Include(m => m.Ratings)
                .Include(m => m.Comments.Where(c => c.IsApproved))
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null)
                return NotFound(ApiResponse.Fail("Фильм не найден"));

            var userId = GetUserId();
            int? userRating = null;
            bool isInWishlist = false;

            if (userId.HasValue)
            {
                userRating = await _context.Ratings
                    .Where(r => r.UserId == userId.Value && r.MovieId == id)
                    .Select(r => (int?)r.Score)
                    .FirstOrDefaultAsync();

                isInWishlist = await _context.Wishlists
                    .AnyAsync(w => w.UserId == userId.Value && w.MovieId == id);
            }

            var result = new MovieDetailsDto
            {
                Id = movie.Id,
                Title = movie.Title,
                Description = movie.Description,
                ReleaseYear = movie.ReleaseYear,
                PosterUrl = movie.PosterUrl,
                ImdbId = movie.ImdbId,
                ImdbRating = movie.ImdbRating,
                Runtime = movie.Runtime,
                Genres = movie.MovieGenres.Select(mg => mg.Genre.Name).ToList(),
                AverageRating = movie.Ratings.Any() ? movie.Ratings.Average(r => r.Score) : null,
                RatingsCount = movie.Ratings.Count,
                UserRating = userRating,
                IsInWishlist = isInWishlist,
                Comments = movie.Comments.Select(c => new CommentDto
                {
                    Id = c.Id,
                    MovieId = c.MovieId,
                    MovieTitle = movie.Title,
                    UserId = c.UserId,
                    UserName = $"{c.User.Name} {c.User.LastName}",
                    UserAvatarUrl = c.User.AvatarUrl,
                    Text = c.Text,
                    CreatedAt = c.CreatedAt,
                    IsApproved = c.IsApproved
                }).ToList()
            };

            return Ok(ApiResponse<MovieDetailsDto>.Ok(result));
        }

        /// <summary>
        /// Поиск фильмов (автодополнение)
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<List<MovieSearchDto>>), 200)]
        public async Task<IActionResult> Search([FromQuery] string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                return Ok(ApiResponse<List<MovieSearchDto>>.Ok(new List<MovieSearchDto>()));

            var movies = await _context.Movies
                .Where(m => m.Title.ToLower().Contains(term.ToLower()))
                .OrderByDescending(m => m.ImdbRating)
                .Take(10)
                .Select(m => new MovieSearchDto
                {
                    Id = m.Id,
                    Title = m.Title,
                    ReleaseYear = m.ReleaseYear,
                    PosterUrl = m.PosterUrl
                })
                .ToListAsync();

            return Ok(ApiResponse<List<MovieSearchDto>>.Ok(movies));
        }

        /// <summary>
        /// Оценить фильм
        /// </summary>
        [HttpPost("{id}/rate")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> RateMovie(int id, [FromBody] CreateRatingDto model)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse.Fail("Не авторизован"));

            if (model.Score < 1 || model.Score > 5)
                return BadRequest(ApiResponse.Fail("Оценка должна быть от 1 до 5"));

            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
                return NotFound(ApiResponse.Fail("Фильм не найден"));

            var existingRating = await _context.Ratings
                .FirstOrDefaultAsync(r => r.UserId == userId.Value && r.MovieId == id);

            if (existingRating != null)
            {
                existingRating.Score = model.Score;
                existingRating.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                var rating = new Rating
                {
                    UserId = userId.Value,
                    MovieId = id,
                    Score = model.Score,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.Ratings.AddAsync(rating);
            }

            await _context.SaveChangesAsync();
            return Ok(ApiResponse.Ok("Оценка сохранена"));
        }

        /// <summary>
        /// Добавить комментарий к фильму
        /// </summary>
        [HttpPost("{id}/comment")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [ProducesResponseType(typeof(ApiResponse<CommentDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> AddComment(int id, [FromBody] CreateCommentDto model)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse.Fail("Не авторизован"));

            if (string.IsNullOrWhiteSpace(model.Text))
                return BadRequest(ApiResponse.Fail("Текст комментария обязателен"));

            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
                return NotFound(ApiResponse.Fail("Фильм не найден"));

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return NotFound(ApiResponse.Fail("Пользователь не найден"));

            var comment = new Comment
            {
                UserId = userId.Value,
                MovieId = id,
                Text = model.Text.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsApproved = false
            };

            await _context.Comments.AddAsync(comment);
            await _context.SaveChangesAsync();

            var result = new CommentDto
            {
                Id = comment.Id,
                MovieId = comment.MovieId,
                MovieTitle = movie.Title,
                UserId = comment.UserId,
                UserName = $"{user.Name} {user.LastName}",
                UserAvatarUrl = user.AvatarUrl,
                Text = comment.Text,
                CreatedAt = comment.CreatedAt,
                IsApproved = comment.IsApproved
            };

            return Ok(ApiResponse<CommentDto>.Ok(result, "Комментарий отправлен на модерацию"));
        }

        /// <summary>
        /// Получить список жанров
        /// </summary>
        [HttpGet("genres")]
        [ProducesResponseType(typeof(ApiResponse<List<GenreDto>>), 200)]
        public async Task<IActionResult> GetGenres()
        {
            var genres = await _context.Genres
                .OrderBy(g => g.Name)
                .Select(g => new GenreDto
                {
                    Id = g.Id,
                    Name = g.Name
                })
                .ToListAsync();

            return Ok(ApiResponse<List<GenreDto>>.Ok(genres));
        }

        /// <summary>
        /// Получить список годов выпуска
        /// </summary>
        [HttpGet("years")]
        [ProducesResponseType(typeof(ApiResponse<List<int>>), 200)]
        public async Task<IActionResult> GetYears()
        {
            var years = await _context.Movies
                .Where(m => m.ReleaseYear.HasValue)
                .Select(m => m.ReleaseYear!.Value)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            return Ok(ApiResponse<List<int>>.Ok(years));
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out int userId))
                return userId;
            return null;
        }
    }
}
