using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using movieRecom.Models.Model;

namespace movieRecom.Controllers
{
    public class AdminController : BaseController
    {
        public AdminController(EducationDbContext context) : base(context) { }

        // Проверка прав администратора
        private async Task<bool> IsAdminAsync()
        {
            var user = await GetCurrentUserAsync();
            return user != null && user.Role == UserRole.Admin;
        }

        // GET: Admin
        public async Task<IActionResult> Index()
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var stats = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalMovies = await _context.Movies.CountAsync(),
                TotalRatings = await _context.Ratings.CountAsync(),
                TotalComments = await _context.Comments.CountAsync(),
                PendingComments = await _context.Comments.CountAsync(c => !c.IsApproved),
                RecentRatings = await _context.Ratings
                    .Include(r => r.User)
                    .Include(r => r.Movie)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(10)
                    .ToListAsync()
            };

            return View(stats);
        }

        // GET: Admin/Movies
        public async Task<IActionResult> Movies(int page = 1)
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            const int pageSize = 20;
            var totalItems = await _context.Movies.CountAsync();

            var movies = await _context.Movies
                .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
                .OrderByDescending(m => m.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(movies);
        }

        // GET: Admin/CreateMovie
        public async Task<IActionResult> CreateMovie()
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            ViewBag.Genres = await _context.Genres.ToListAsync();
            return View(new Movie());
        }

        // POST: Admin/CreateMovie
        [HttpPost]
        public async Task<IActionResult> CreateMovie(Movie movie, int[] selectedGenres)
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                _context.Movies.Add(movie);
                await _context.SaveChangesAsync();

                // Add genres
                foreach (var genreId in selectedGenres)
                {
                    _context.MovieGenres.Add(new MovieGenre
                    {
                        MovieId = movie.Id,
                        GenreId = genreId
                    });
                }
                await _context.SaveChangesAsync();

                Notif_Success("Фильм успешно добавлен!");
                return RedirectToAction("Movies");
            }

            ViewBag.Genres = await _context.Genres.ToListAsync();
            return View(movie);
        }

        // GET: Admin/EditMovie/5
        public async Task<IActionResult> EditMovie(int id)
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var movie = await _context.Movies
                .Include(m => m.MovieGenres)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null)
            {
                return NotFound();
            }

            ViewBag.Genres = await _context.Genres.ToListAsync();
            ViewBag.SelectedGenres = movie.MovieGenres.Select(mg => mg.GenreId).ToList();
            return View(movie);
        }

        // POST: Admin/EditMovie/5
        [HttpPost]
        public async Task<IActionResult> EditMovie(int id, Movie movie, int[] selectedGenres)
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            if (id != movie.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(movie);

                    // Update genres
                    var existingGenres = await _context.MovieGenres
                        .Where(mg => mg.MovieId == id)
                        .ToListAsync();
                    _context.MovieGenres.RemoveRange(existingGenres);

                    foreach (var genreId in selectedGenres)
                    {
                        _context.MovieGenres.Add(new MovieGenre
                        {
                            MovieId = id,
                            GenreId = genreId
                        });
                    }

                    await _context.SaveChangesAsync();
                    Notif_Success("Фильм успешно обновлён!");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Movies.AnyAsync(m => m.Id == id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction("Movies");
            }

            ViewBag.Genres = await _context.Genres.ToListAsync();
            return View(movie);
        }

        // POST: Admin/DeleteMovie/5
        [HttpPost]
        public async Task<IActionResult> DeleteMovie(int id)
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var movie = await _context.Movies.FindAsync(id);
            if (movie != null)
            {
                _context.Movies.Remove(movie);
                await _context.SaveChangesAsync();
                Notif_Success("Фильм удалён!");
            }

            return RedirectToAction("Movies");
        }

        // GET: Admin/Comments
        public async Task<IActionResult> Comments(bool pending = true)
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var comments = await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Movie)
                .Where(c => pending ? !c.IsApproved : c.IsApproved)
                .OrderByDescending(c => c.CreatedAt)
                .Take(50)
                .ToListAsync();

            ViewBag.ShowPending = pending;
            return View(comments);
        }

        // POST: Admin/ApproveComment
        [HttpPost]
        public async Task<IActionResult> ApproveComment(int id)
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var comment = await _context.Comments.FindAsync(id);
            if (comment != null)
            {
                comment.IsApproved = true;
                await _context.SaveChangesAsync();
                Notif_Success("Комментарий одобрен!");
            }

            return RedirectToAction("Comments");
        }

        // POST: Admin/DeleteComment
        [HttpPost]
        public async Task<IActionResult> DeleteComment(int id)
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var comment = await _context.Comments.FindAsync(id);
            if (comment != null)
            {
                _context.Comments.Remove(comment);
                await _context.SaveChangesAsync();
                Notif_Info("Комментарий удалён");
            }

            return RedirectToAction("Comments");
        }

        // GET: Admin/Users
        public async Task<IActionResult> Users()
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var users = await _context.Users
                .Select(u => new UserAdminViewModel
                {
                    Id = u.Id,
                    Name = u.Name,
                    LastName = u.LastName,
                    Email = u.Email,
                    Role = u.Role,
                    RatingsCount = u.Ratings.Count,
                    WishlistCount = u.WishlistItems.Count
                })
                .OrderByDescending(u => u.Id)
                .ToListAsync();

            return View(users);
        }

        // POST: Admin/ChangeRole
        [HttpPost]
        public async Task<IActionResult> ChangeRole(int userId, string role)
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                Notif_Error("Пользователь не найден");
                return RedirectToAction("Users");
            }

            // Нельзя изменить роль самому себе
            var currentUser = await GetCurrentUserAsync();
            if (currentUser?.Id == userId)
            {
                Notif_Error("Нельзя изменить свою роль");
                return RedirectToAction("Users");
            }

            if (Enum.TryParse<UserRole>(role, out var newRole))
            {
                user.Role = newRole;
                await _context.SaveChangesAsync();
                Notif_Success($"Роль пользователя {user.Name} изменена на {newRole}");
            }
            else
            {
                Notif_Error("Недопустимая роль");
            }

            return RedirectToAction("Users");
        }
    }

    // ViewModels для админки
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalMovies { get; set; }
        public int TotalRatings { get; set; }
        public int TotalComments { get; set; }
        public int PendingComments { get; set; }
        public List<Rating> RecentRatings { get; set; } = new();
    }

    public class UserAdminViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public int RatingsCount { get; set; }
        public int WishlistCount { get; set; }
    }
}
