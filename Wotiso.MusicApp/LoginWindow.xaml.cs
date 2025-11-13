using System.Text;
using System.Windows;
using Wotiso.MusicApp.BLL.Services;
using Wotiso.MusicApp.DAL.Entities;

namespace Wotiso.MusicApp.Views
{
    public partial class LoginWindow : Window
    {
        private UserService _service = new();
        private static int WrongPasswordCount = 0;
        private const int MaxWrongAttempts = 5;
        public LoginWindow()
        {
            InitializeComponent();
        }

        public LoginWindow(String email, String password)
        {
            InitializeComponent();
            EmailText.Text = email;
            PasswordText.Password = password;
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            var email = EmailText.Text.Trim();
            var pass = PasswordText.Password.Trim();

            if (WrongPasswordCount >= MaxWrongAttempts)
            {
                MessageBox.Show("You have entered the wrong password too many times.\nPlease restart the application to try again.",
                                "Account Locked",
                                MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            var userByEmail = _service.GetByEmail(email);
            if (userByEmail == null)
            {
                MessageBox.Show("Email does not exist!",
                                "Login Error",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var user = _service.Login(email, pass);

            if (user == null)
            {
                WrongPasswordCount++;

                int remain = MaxWrongAttempts - WrongPasswordCount;

                if (remain > 0)
                {
                    MessageBox.Show($"Incorrect password!\nYou have {remain} attempts left.",
                                    "Invalid Password",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show("Too many incorrect attempts!\nPlease restart the application to try again.",
                                    "Account Locked",
                                    MessageBoxButton.OK, MessageBoxImage.Stop);
                }

                return;
            }

            WrongPasswordCount = 0;

            var main = new MainWindow();
            main.Show();

            this.Close();
        }

        private void OpenRegister_Click(object sender, RoutedEventArgs e)
        {
            var reg = new RegisterWindow();
            reg.ShowDialog();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
