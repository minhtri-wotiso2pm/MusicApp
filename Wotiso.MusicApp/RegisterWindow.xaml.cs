using System.Text.RegularExpressions;
using System.Windows;
using Wotiso.MusicApp.BLL.Services;

namespace Wotiso.MusicApp.Views
{
    public partial class RegisterWindow : Window
    {
        private readonly UserService _service = new();

        public RegisterWindow()
        {
            InitializeComponent();
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            var name = NameText.Text.Trim();
            var email = EmailText.Text.Trim();
            var pass = PasswordText.Password.Trim();
            var confirm = ConfirmPasswordText.Password.Trim();


            if (name.Length < 4 || name.Length > 30)
            {
                MessageBox.Show("Name must be between 4 and 30 characters.",
                                "Invalid Name",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsValidEmail(email))
            {
                MessageBox.Show("Invalid email format!",
                                "Invalid Email",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (pass.Length < 8)
            {
                MessageBox.Show("Password must be at least 8 characters long.",
                                "Invalid Password",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (pass != confirm)
            {
                MessageBox.Show("Passwords do not match!",
                                "Password Error",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }


            if (_service.Register(name, email, pass))
            {
                var login = new LoginWindow(email, pass);
                login.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("This email is already registered!",
                                "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
