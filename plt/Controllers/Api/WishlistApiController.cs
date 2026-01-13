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
    public class WishlistController : ControllerBase
    {
        private readonly EducationDbContext _context;

        public WishlistController(EducationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получить wishlist пользователя
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<WishlistDto>), 200)]
        public async Task<IActionResult> GetWishlist()
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse.Fail("Не авторизован"));

            var items = await _context.Wishlists
                .Where(w => w.UserId == userId.Value)
                .Include(w => w.Movie)
                    .ThenInclude(m => m.MovieGenres)
                        .ThenInclude(mg => mg.Genre)
                .OrderByDescending(w => w.AddedAt)
                .Select(w => new WishlistItemDto
                {
                    MovieId = w.MovieId,
                    Title = w.Movie.Title,
                    PosterUrl = w.Movie.PosterUrl,
                    ReleaseYear = w.Movie.ReleaseYear,
                    Genres = w.Movie.MovieGenres.Select(mg => mg.Genre.Name).ToList(),
                    AddedAt = w.AddedAt
                })
                .ToListAsync();

            var result = new WishlistDto
            {
                Items = items,
                TotalCount = items.Count
            };

            return Ok(ApiResponse<WishlistDto>.Ok(result));
        }

        /// <summary>
        /// Добавить фильм в wishlist
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> AddToWishlist([FromBody] AddToWishlistDto model)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse.Fail("Не авторизован"));

            var movie = await _context.Movies.FindAsync(model.MovieId);
            if (movie == null)
                return NotFound(ApiResponse.Fail("Фильм не найден"));

            var exists = await _context.Wishlists
                .AnyAsync(w => w.UserId == userId.Value && w.MovieId == model.MovieId);

            if (exists)
                return BadRequest(ApiResponse.Fail("Фильм уже в wishlist"));

            var wishlistItem = new Wishlist
            {
                UserId = userId.Value,
                MovieId = model.MovieId,
                AddedAt = DateTime.UtcNow
            };

            await _context.Wishlists.AddAsync(wishlistItem);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Ok("Фильм добавлен в wishlist"));
        }

        /// <summary>
        /// Удалить фильм из wishlist
        /// </summary>
        [HttpDelete("{movieId}")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> RemoveFromWishlist(int movieId)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse.Fail("Не авторизован"));

            var wishlistItem = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId.Value && w.MovieId == movieId);

            if (wishlistItem == null)
                return NotFound(ApiResponse.Fail("Фильм не найден в wishlist"));

            _context.Wishlists.Remove(wishlistItem);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Ok("Фильм удалён из wishlist"));
        }

        /// <summary>
        /// Проверить, есть ли фильм в wishlist
        /// </summary>
        [HttpGet("check/{movieId}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> CheckInWishlist(int movieId)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse.Fail("Не авторизован"));

            var exists = await _context.Wishlists
                .AnyAsync(w => w.UserId == userId.Value && w.MovieId == movieId);

            return Ok(ApiResponse<bool>.Ok(exists));
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
