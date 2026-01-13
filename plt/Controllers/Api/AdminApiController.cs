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
    public class AdminController : ControllerBase
    {
        private readonly EducationDbContext _context;

        public AdminController(EducationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получить статистику системы
        /// </summary>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(ApiResponse<AdminStatsDto>), 200)]
        public async Task<IActionResult> GetStats()
        {
            if (!await IsAdmin())
                return Forbid();

            var stats = new AdminStatsDto
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalMovies = await _context.Movies.CountAsync(),
                TotalRatings = await _context.Ratings.CountAsync(),
                TotalComments = await _context.Comments.CountAsync(),
                PendingComments = await _context.Comments.CountAsync(c => !c.IsApproved),
                TotalWishlists = await _context.Wishlists.CountAsync()
            };

            return Ok(ApiResponse<AdminStatsDto>.Ok(stats));
        }

        /// <summary>
        /// Получить список пользователей
        /// </summary>
        [HttpGet("users")]
        [ProducesResponseType(typeof(ApiResponse<List<AdminUserDto>>), 200)]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (!await IsAdmin())
                return Forbid();

            var users = await _context.Users
                .Include(u => u.Ratings)
                .Include(u => u.WishlistItems)
                .OrderByDescending(u => u.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new AdminUserDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    LastName = u.LastName,
                    Email = u.Email,
                    Role = u.Role.ToString(),
                    RatingsCount = u.Ratings.Count,
                    WishlistCount = u.WishlistItems.Count
                })
                .ToListAsync();

            return Ok(ApiResponse<List<AdminUserDto>>.Ok(users));
        }

        /// <summary>
        /// Изменить роль пользователя
        /// </summary>
        [HttpPut("users/{userId}/role")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        public async Task<IActionResult> ChangeUserRole(int userId, [FromBody] ChangeRoleDto model)
        {
            if (!await IsAdmin())
                return Forbid();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(ApiResponse.Fail("Пользователь не найден"));

            if (!Enum.TryParse<UserRole>(model.Role, out var role))
                return BadRequest(ApiResponse.Fail("Недопустимая роль"));

            user.Role = role;
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Ok($"Роль пользователя изменена на {role}"));
        }

        /// <summary>
        /// Создать фильм
        /// </summary>
        [HttpPost("movies")]
        [ProducesResponseType(typeof(ApiResponse<MovieDto>), 200)]
        public async Task<IActionResult> CreateMovie([FromBody] CreateMovieDto model)
        {
            if (!await IsAdmin())
                return Forbid();

            var movie = new Movie
            {
                Title = model.Title,
                Description = model.Description,
                ReleaseYear = model.ReleaseYear,
                PosterUrl = model.PosterUrl,
                ImdbId = model.ImdbId,
                ImdbRating = model.ImdbRating,
                Runtime = model.Runtime
            };

            await _context.Movies.AddAsync(movie);
            await _context.SaveChangesAsync();

            // Добавляем жанры
            if (model.GenreIds.Any())
            {
                var movieGenres = model.GenreIds.Select(gId => new MovieGenre
                {
                    MovieId = movie.Id,
                    GenreId = gId
                });
                await _context.MovieGenres.AddRangeAsync(movieGenres);
                await _context.SaveChangesAsync();
            }

            var result = new MovieDto
            {
                Id = movie.Id,
                Title = movie.Title,
                Description = movie.Description,
                ReleaseYear = movie.ReleaseYear,
                PosterUrl = movie.PosterUrl,
                ImdbId = movie.ImdbId,
                ImdbRating = movie.ImdbRating,
                Runtime = movie.Runtime,
                Genres = await _context.MovieGenres
                    .Where(mg => mg.MovieId == movie.Id)
                    .Select(mg => mg.Genre.Name)
                    .ToListAsync()
            };

            return Ok(ApiResponse<MovieDto>.Ok(result, "Фильм создан"));
        }

        /// <summary>
        /// Обновить фильм
        /// </summary>
        [HttpPut("movies/{id}")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        public async Task<IActionResult> UpdateMovie(int id, [FromBody] UpdateMovieDto model)
        {
            if (!await IsAdmin())
                return Forbid();

            var movie = await _context.Movies
                .Include(m => m.MovieGenres)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null)
                return NotFound(ApiResponse.Fail("Фильм не найден"));

            movie.Title = model.Title;
            movie.Description = model.Description;
            movie.ReleaseYear = model.ReleaseYear;
            movie.PosterUrl = model.PosterUrl;
            movie.ImdbId = model.ImdbId;
            movie.ImdbRating = model.ImdbRating;
            movie.Runtime = model.Runtime;

            // Обновляем жанры
            _context.MovieGenres.RemoveRange(movie.MovieGenres);
            if (model.GenreIds.Any())
            {
                var movieGenres = model.GenreIds.Select(gId => new MovieGenre
                {
                    MovieId = movie.Id,
                    GenreId = gId
                });
                await _context.MovieGenres.AddRangeAsync(movieGenres);
            }

            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Ok("Фильм обновлён"));
        }

        /// <summary>
        /// Удалить фильм
        /// </summary>
        [HttpDelete("movies/{id}")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        public async Task<IActionResult> DeleteMovie(int id)
        {
            if (!await IsAdmin())
                return Forbid();

            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
                return NotFound(ApiResponse.Fail("Фильм не найден"));

            _context.Movies.Remove(movie);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Ok("Фильм удалён"));
        }

        /// <summary>
        /// Получить комментарии на модерацию
        /// </summary>
        [HttpGet("comments")]
        [ProducesResponseType(typeof(ApiResponse<List<CommentDto>>), 200)]
        public async Task<IActionResult> GetComments([FromQuery] bool? approved = null)
        {
            if (!await IsAdmin())
                return Forbid();

            var query = _context.Comments
                .Include(c => c.User)
                .Include(c => c.Movie)
                .AsQueryable();

            if (approved.HasValue)
                query = query.Where(c => c.IsApproved == approved.Value);

            var comments = await query
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    MovieId = c.MovieId,
                    MovieTitle = c.Movie.Title,
                    UserId = c.UserId,
                    UserName = $"{c.User.Name} {c.User.LastName}",
                    UserAvatarUrl = c.User.AvatarUrl,
                    Text = c.Text,
                    CreatedAt = c.CreatedAt,
                    IsApproved = c.IsApproved
                })
                .ToListAsync();

            return Ok(ApiResponse<List<CommentDto>>.Ok(comments));
        }

        /// <summary>
        /// Одобрить комментарий
        /// </summary>
        [HttpPut("comments/{id}/approve")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        public async Task<IActionResult> ApproveComment(int id)
        {
            if (!await IsAdmin())
                return Forbid();

            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
                return NotFound(ApiResponse.Fail("Комментарий не найден"));

            comment.IsApproved = true;
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Ok("Комментарий одобрен"));
        }

        /// <summary>
        /// Удалить комментарий
        /// </summary>
        [HttpDelete("comments/{id}")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        public async Task<IActionResult> DeleteComment(int id)
        {
            if (!await IsAdmin())
                return Forbid();

            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
                return NotFound(ApiResponse.Fail("Комментарий не найден"));

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Ok("Комментарий удалён"));
        }

        /// <summary>
        /// Получить последние оценки
        /// </summary>
        [HttpGet("ratings/recent")]
        [ProducesResponseType(typeof(ApiResponse<List<RatingDto>>), 200)]
        public async Task<IActionResult> GetRecentRatings([FromQuery] int count = 20)
        {
            if (!await IsAdmin())
                return Forbid();

            var ratings = await _context.Ratings
                .Include(r => r.User)
                .Include(r => r.Movie)
                .OrderByDescending(r => r.CreatedAt)
                .Take(count)
                .Select(r => new RatingDto
                {
                    MovieId = r.MovieId,
                    MovieTitle = r.Movie.Title,
                    Score = r.Score,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<List<RatingDto>>.Ok(ratings));
        }

        /// <summary>
        /// Создать жанр
        /// </summary>
        [HttpPost("genres")]
        [ProducesResponseType(typeof(ApiResponse<GenreDto>), 200)]
        public async Task<IActionResult> CreateGenre([FromBody] GenreDto model)
        {
            if (!await IsAdmin())
                return Forbid();

            if (await _context.Genres.AnyAsync(g => g.Name == model.Name))
                return BadRequest(ApiResponse.Fail("Жанр уже существует"));

            var genre = new Genre { Name = model.Name };
            await _context.Genres.AddAsync(genre);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<GenreDto>.Ok(new GenreDto { Id = genre.Id, Name = genre.Name }));
        }

        private async Task<bool> IsAdmin()
        {
            var userId = GetUserId();
            if (userId == null) return false;

            var user = await _context.Users.FindAsync(userId);
            return user?.Role == UserRole.Admin;
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out int userId))
                return userId;
            return null;
        }
    }

    // DTO классы для Admin
    public class AdminStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalMovies { get; set; }
        public int TotalRatings { get; set; }
        public int TotalComments { get; set; }
        public int PendingComments { get; set; }
        public int TotalWishlists { get; set; }
    }

    public class AdminUserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int RatingsCount { get; set; }
        public int WishlistCount { get; set; }
    }

    public class ChangeRoleDto
    {
        public string Role { get; set; } = string.Empty;
    }
}
