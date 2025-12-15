using System.ComponentModel.DataAnnotations;
using plt.Models.Model;


namespace plt.Models.ViewModel
{
    public class RegisterViewModel : BaseViewModel
    {
        private string _name = string.Empty;
        private string _lastName = string.Empty;
        private string _email = string.Empty;
        private string _password = string.Empty;


        [Required(ErrorMessage = "Имя обязательно")]
        [MinLength(2, ErrorMessage = "Имя должно содержать минимум 2 символа")]
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value ?? string.Empty);
        }

        [Required(ErrorMessage = "Фамилия обязательна")]
        [MinLength(2, ErrorMessage = "Фамилия должна содержать минимум 2 символа")]
        public string LastName
        {
            get => _lastName;
            set => SetProperty(ref _lastName, value ?? string.Empty);
        }

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value ?? string.Empty);
        }


        [Required(ErrorMessage = "Пароль обязателен")]
        [MinLength(8, ErrorMessage = "Пароль должен содержать минимум 8 символов")]
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value ?? string.Empty);
        }

        public string FullName => $"{Name} {LastName}".Trim();

        public RegisterViewModel()
        {
            InitializeTracking();
        }
    }
}
