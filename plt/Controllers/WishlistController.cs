using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using movieRecom.Models.Model;
using movieRecom.Models.ViewModel;

namespace movieRecom.Controllers
{
    public class WishlistController : BaseController
    {
        public WishlistController(EducationDbContext context) : base(context) { }

        // GET: Wishlist
        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var wishlistItems = await _context.Wishlists
                .Where(w => w.UserId == user.Id)
                .Include(w => w.Movie)
                .ThenInclude(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
                .OrderByDescending(w => w.AddedAt)
                .ToListAsync();

            return View(wishlistItems);
        }

        // POST: Wishlist/Add
        [HttpPost]
        public async Task<IActionResult> Add(int movieId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var exists = await _context.Wishlists
                .AnyAsync(w => w.MovieId == movieId && w.UserId == user.Id);

            if (!exists)
            {
                var wishlistItem = new Wishlist
                {
                    MovieId = movieId,
                    UserId = user.Id
                };
                _context.Wishlists.Add(wishlistItem);
                await _context.SaveChangesAsync();
                Notif_Success("Фильм добавлен в список желаний!");
            }

            return RedirectToAction("Details", "Movies", new { id = movieId });
        }

        // POST: Wishlist/Remove
        [HttpPost]
        public async Task<IActionResult> Remove(int movieId, string? returnUrl = null)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var wishlistItem = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.MovieId == movieId && w.UserId == user.Id);

            if (wishlistItem != null)
            {
                _context.Wishlists.Remove(wishlistItem);
                await _context.SaveChangesAsync();
                Notif_Info("Фильм удалён из списка желаний");
            }

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Details", "Movies", new { id = movieId });
        }
    }
}
