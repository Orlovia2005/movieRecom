using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using movieRecom.Models.Model;

namespace movieRecom.Controllers
{
    public class HistoryController : BaseController
    {
        public HistoryController(EducationDbContext context) : base(context) { }

        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var viewModel = new HistoryViewModel
            {
                Ratings = await _context.Ratings
                    .Where(r => r.UserId == user.Id)
                    .Include(r => r.Movie)
                        .ThenInclude(m => m.MovieGenres)
                            .ThenInclude(mg => mg.Genre)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync(),

                WishlistItems = await _context.Wishlists
                    .Where(w => w.UserId == user.Id)
                    .Include(w => w.Movie)
                        .ThenInclude(m => m.MovieGenres)
                            .ThenInclude(mg => mg.Genre)
                    .OrderByDescending(w => w.AddedAt)
                    .ToListAsync(),

                Comments = await _context.Comments
                    .Where(c => c.UserId == user.Id)
                    .Include(c => c.Movie)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync()
            };

            return View(viewModel);
        }
    }

    public class HistoryViewModel
    {
        public List<Rating> Ratings { get; set; } = new();
        public List<Wishlist> WishlistItems { get; set; } = new();
        public List<Comment> Comments { get; set; } = new();

        public int TotalActivities => Ratings.Count + WishlistItems.Count + Comments.Count;
    }
}
