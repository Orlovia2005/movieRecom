using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using plt.Models.Model; // ваш DbContext

namespace plt.Models.Model
{

    public interface ICurrentUserService
    {

        Task<User?> GetUserAsync();

    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly EducationDbContext _context;
        private User? _cachedUser;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor, EducationDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

        private int? UserId =>
            int.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (int?)null;

        public async Task<User?> GetUserAsync()
        {
            if (_cachedUser != null) return _cachedUser;

            if (UserId == null) return null;

            _cachedUser = await _context.Users
                .Include(u => u.Role) // чтобы сразу загрузить роль
                .FirstOrDefaultAsync(u => u.Id == UserId);

            return _cachedUser;
        }


    }
}