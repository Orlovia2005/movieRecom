using System.ComponentModel.DataAnnotations;

namespace movieRecom.Models.DTO
{
    // Регистрация
    public class RegisterDto
    {
        [Required(ErrorMessage = "Имя обязательно")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Фамилия обязательна")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен")]
        [MinLength(6, ErrorMessage = "Пароль должен быть минимум 6 символов")]
        public string Password { get; set; } = string.Empty;
    }

    // Логин
    public class LoginDto
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен")]
        public string Password { get; set; } = string.Empty;
    }

    // Ответ с токенами
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; } = null!;
    }

    // Refresh токен
    public class RefreshTokenDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    // Восстановление пароля
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }

    // Смена пароля
    public class ChangePasswordDto
    {
        [Required]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
