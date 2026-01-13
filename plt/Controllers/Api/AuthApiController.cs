using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using movieRecom.Models.DTO;
using movieRecom.Models.Model;
using movieRecom.Services;

namespace movieRecom.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly EducationDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _configuration;

        public AuthController(EducationDbContext context, IJwtService jwtService, IConfiguration configuration)
        {
            _context = context;
            _jwtService = jwtService;
            _configuration = configuration;
        }

        /// <summary>
        /// Регистрация нового пользователя
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse.Fail("Ошибка валидации", errors));
            }

            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                return BadRequest(ApiResponse.Fail("Пользователь с таким email уже существует"));
            }

            var hasher = new PasswordHasher<User>();
            var user = new User
            {
                Name = model.Name,
                LastName = model.LastName,
                Email = model.Email,
                Password = hasher.HashPassword(null!, model.Password),
                AvatarUrl = "/Images/BaseAvatar.jpg",
                Role = UserRole.User
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var response = await GenerateAuthResponse(user);
            return Ok(ApiResponse<AuthResponseDto>.Ok(response, "Регистрация успешна"));
        }

        /// <summary>
        /// Авторизация пользователя
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse.Fail("Ошибка валидации", errors));
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                return Unauthorized(ApiResponse.Fail("Неверный email или пароль"));
            }

            var hasher = new PasswordHasher<User>();
            if (hasher.VerifyHashedPassword(user, user.Password, model.Password) == PasswordVerificationResult.Failed)
            {
                return Unauthorized(ApiResponse.Fail("Неверный email или пароль"));
            }

            var response = await GenerateAuthResponse(user);
            return Ok(ApiResponse<AuthResponseDto>.Ok(response, "Авторизация успешна"));
        }

        /// <summary>
        /// Обновление токена
        /// </summary>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto model)
        {
            var refreshToken = await _context.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Token == model.RefreshToken && !r.IsRevoked);

            if (refreshToken == null || refreshToken.ExpiresAt < DateTime.UtcNow)
            {
                return Unauthorized(ApiResponse.Fail("Недействительный или истёкший refresh token"));
            }

            // Отзываем старый токен
            refreshToken.IsRevoked = true;

            var response = await GenerateAuthResponse(refreshToken.User);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<AuthResponseDto>.Ok(response, "Токен обновлён"));
        }

        /// <summary>
        /// Выход (отзыв refresh token)
        /// </summary>
        [HttpPost("logout")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenDto model)
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == model.RefreshToken);

            if (refreshToken != null)
            {
                refreshToken.IsRevoked = true;
                await _context.SaveChangesAsync();
            }

            return Ok(ApiResponse.Ok("Выход выполнен"));
        }

        /// <summary>
        /// Получить текущего пользователя
        /// </summary>
        [HttpGet("me")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), 200)]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse.Fail("Не авторизован"));

            var user = await _context.Users
                .Include(u => u.Ratings)
                .Include(u => u.WishlistItems)
                .Include(u => u.Comments)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(ApiResponse.Fail("Пользователь не найден"));

            var profile = new UserProfileDto
            {
                Id = user.Id,
                Name = user.Name,
                LastName = user.LastName,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                Role = user.Role.ToString(),
                RatingsCount = user.Ratings.Count,
                WishlistCount = user.WishlistItems.Count,
                CommentsCount = user.Comments.Count
            };

            return Ok(ApiResponse<UserProfileDto>.Ok(profile));
        }

        /// <summary>
        /// Смена пароля
        /// </summary>
        [HttpPost("change-password")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse.Fail("Не авторизован"));

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(ApiResponse.Fail("Пользователь не найден"));

            var hasher = new PasswordHasher<User>();
            if (hasher.VerifyHashedPassword(user, user.Password, model.OldPassword) == PasswordVerificationResult.Failed)
            {
                return BadRequest(ApiResponse.Fail("Неверный старый пароль"));
            }

            user.Password = hasher.HashPassword(user, model.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Ok("Пароль успешно изменён"));
        }

        private async Task<AuthResponseDto> GenerateAuthResponse(User user)
        {
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            var expireDays = int.Parse(_configuration["Jwt:RefreshExpireDays"] ?? "7");

            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(expireDays),
                CreatedAt = DateTime.UtcNow
            };

            await _context.RefreshTokens.AddAsync(refreshTokenEntity);
            await _context.SaveChangesAsync();

            var expireMinutes = int.Parse(_configuration["Jwt:ExpireMinutes"] ?? "60");

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expireMinutes),
                User = new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    LastName = user.LastName,
                    Email = user.Email,
                    AvatarUrl = user.AvatarUrl,
                    Role = user.Role.ToString()
                }
            };
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
