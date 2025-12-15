using System.ComponentModel.DataAnnotations;


namespace plt.Models.ViewModel
{
    public class LoginViewModel:  BaseViewModel
    {
        
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;


        [Required]
        public string Password { get; set; } = string.Empty;

    }

}
