
namespace plt.Models.ViewModel
{
    public class ProfileViewModel : BaseViewModel
    {

        private string _firstName;
        private string _secondName;
        private string _email;
        private string _oldPassword;
        private string _newpassword;
        private string _confirmNewpassword;
        private string _avatarUrl;
        private int _id;

        public string FirstName
        {
            get => _firstName;
            set => SetProperty(ref _firstName, value);
        }
        public string SecondName
        {
            get => _secondName;
            set => SetProperty(ref _secondName, value);
        }
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }
        public string OldPassword
        {
            get => _oldPassword;
            set => SetProperty(ref _oldPassword, value);
        }
        public string AvatarUrl
        {
            get => _avatarUrl;
            set => SetProperty(ref _avatarUrl, value);
        }
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }
        public string NewPassword
        {
            get => _newpassword;
            set => SetProperty(ref _newpassword, value);
        }
        public string ConfirmNewPassword
        {
            get => _confirmNewpassword;
            set=> SetProperty(ref _confirmNewpassword, value);
        }

    }
}
