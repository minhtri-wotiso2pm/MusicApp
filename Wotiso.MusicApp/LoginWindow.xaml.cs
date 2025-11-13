using System.Text;
using System.Windows;
using Wotiso.MusicApp.BLL.Services;
using Wotiso.MusicApp.DAL;
using Wotiso.MusicApp.DAL.Entities;
using Wotiso.MusicApp.DAL.Repositories;

namespace Wotiso.MusicApp.Views
{
    public partial class LoginWindow : Window
    {
        private readonly MusicPlayerDbContext _context;
        private readonly UserRepository _userRepo;
        private readonly SongRepository _songRepo;
        private readonly PlaylistRepo _playlistRepo;

        private readonly UserService _userService;
        private readonly MusicService _musicService;
        private readonly PlaylistService _playlistService;
        private static int WrongPasswordCount = 0;
        private const int MaxWrongAttempts = 5;
        public LoginWindow()
        {
            InitializeComponent();
            try
            {
                _context = new MusicPlayerDbContext();

                _userRepo = new UserRepository(_context);
                _songRepo = new SongRepository(_context);
                _playlistRepo = new PlaylistRepo(_context);

                // Giả sử các Service của bạn đều có Constructor nhận Repo
                _userService = new UserService(_userRepo);
                _musicService = new MusicService(_songRepo);
                _playlistService = new PlaylistService(_playlistRepo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Can not connect Database.\n Error: {ex.Message}",
                                "Connection", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        public LoginWindow(String email, String password):this()
        {
            
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

            var userByEmail = _userService.GetByEmail(email);
            if (userByEmail == null)
            {
                MessageBox.Show("Email does not exist!",
                                "Login Error",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var user = _userService.Login(email, pass);

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

            var main = new MainWindow(user, _musicService, _playlistService);
            main.Show();

            this.Close();
        }

        private void OpenRegister_Click(object sender, RoutedEventArgs e)
        {
            var reg = new RegisterWindow(_userService);
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
