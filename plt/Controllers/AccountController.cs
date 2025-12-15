using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using plt.Models.Model;
using plt.Models.ViewModel;
using System.Security.Claims;

namespace plt.Controllers
{
    public class AccountController : BaseController
    {
        public AccountController(EducationDbContext context) : base(context) { }

        [HttpGet]
        public ActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var hasher = new PasswordHasher<User>();
            var user = new User
            {
                Name = model.Name,
                LastName = model.LastName,
                Email = model.Email,
                RoleId = model.Roleid,
                Password = hasher.HashPassword(null!, model.Password),
                AvatarUrl = "/Images/BaseAvatar.jpg"
            };

            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                Notif_Info("Пользователь с таким email уже существует.");
                return View(model);
            }

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // ✅ сразу авторизуем нового пользователя
            await SignInUser(user);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public ActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "Неверный логин или пароль");
                return View(model);
            }

            var hasher = new PasswordHasher<User>();
            if (hasher.VerifyHashedPassword(user, user.Password, model.Password) == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Неверный логин или пароль");
                return View(model);
            }

            // ✅ используем тот же метод
            await SignInUser(user);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await GetCurrentUserAsync();
            ProfileViewModel model = new ProfileViewModel();
            if (user != null)
            {
                model.Email = user.Email;
                model.FirstName = user.Name;
                model.SecondName = user.LastName;
                model.AvatarUrl = user.AvatarUrl;
                model.Id = user.Id;
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model, IFormFile avatar)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return NotFound();

            // обновляем данные
            user.Name = model.FirstName;
            user.LastName = model.SecondName;
            user.Email = model.Email;

            if (avatar != null && avatar.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(avatar.FileName)}";
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatar.CopyToAsync(stream);
                }

                user.AvatarUrl = $"/Images/{fileName}";
            }

            _context.Update(user);
            await _context.SaveChangesAsync();

            // ⚡ обновляем куки с новыми клаймами
            await SignInUser(user);

            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePassword(ProfileViewModel model)
        {
            // Получаем текущего пользователя
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                Notif_Error("Пользователь не найден");
                return RedirectToAction("Profile");
            }

            // Проверяем, что старый пароль указан
            if (string.IsNullOrEmpty(model.OldPassword))
            {
                Notif_Error("Введите старый пароль");
                return RedirectToAction("Profile");
            }

            // Проверяем старый пароль
            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(user, user.Password, model.OldPassword);

            if (result == PasswordVerificationResult.Failed)
            {
                Notif_Error("Старый пароль неверный");
                return RedirectToAction("Profile");
            }

            // Проверяем, что новые пароли совпадают
            if (string.IsNullOrEmpty(model.NewPassword) || model.NewPassword != model.ConfirmNewPassword)
            {
                Notif_Error("Новые пароли не совпадают");
                return RedirectToAction("Profile");
            }

            // Проверяем сложность пароля (опционально)
            if (model.NewPassword.Length < 6)
            {
                Notif_Error("Пароль должен содержать минимум 6 символов");
                return RedirectToAction("Profile");
            }

            // Обновляем пароль
            user.Password = hasher.HashPassword(user, model.NewPassword);
            await _context.SaveChangesAsync();

            Notif_Success("Пароль успешно обновлён!");
            return RedirectToAction("Profile");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }


        // общий метод авторизации
        private async Task SignInUser(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name ?? string.Empty),
                new Claim("last_name", user.LastName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim("avatar_url", user.AvatarUrl ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role?.RoleType ?? string.Empty),
                new Claim("role_id", user.RoleId.ToString()),
                new Claim("Base_Price", user.BasePrice.ToString()),
                

            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
                });
        }
    }
}
