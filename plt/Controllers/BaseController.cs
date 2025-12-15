using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using plt.Models.Model;

namespace plt.Controllers
{
    public abstract class BaseController : Controller
    {
        protected readonly EducationDbContext _context;

        protected BaseController(EducationDbContext context)
        {
            _context = context;
        }

        private User? _cachedUser;

        protected async Task<User?> GetCurrentUserAsync()
        {
            if (_cachedUser != null)
                return _cachedUser;

            if (User?.Identity?.IsAuthenticated != true)
                return null;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return null;

            _cachedUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == int.Parse(userId));

            return _cachedUser;
        }


        protected int? CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (int?)null;

        protected string? CurrentUserRole =>
            User.FindFirstValue(ClaimTypes.Role);
        protected void Notif_Success(string message)
        {
            TempData["SuccessMessage"] = message;
        }

        protected void Notif_Error(string message)
        {
            TempData["ErrorMessage"] = message;
        }

        protected void Notif_Info(string message)
        {
            TempData["InfoMessage"] = message;
        }
    }
}
