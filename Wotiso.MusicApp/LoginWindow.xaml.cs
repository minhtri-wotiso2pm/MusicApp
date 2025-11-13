using System.Text;
using System.Windows;
using Wotiso.MusicApp.BLL.Services;
using Wotiso.MusicApp.DAL;
using Wotiso.MusicApp.DAL.Entities;
using Wotiso.MusicApp.DAL.Repositories;

namespace Wotiso.MusicApp.Views
{
    /// <summary>
    /// ==================== LOGIN WINDOW - CỬA SỔ ĐĂNG NHẬP ====================
    /// Window đầu tiên user nhìn thấy khi mở app
    /// 
    /// CHỨC NĂNG:
    /// 1. Hiển thị form đăng nhập (Email + Password)
    /// 2. Validate thông tin đăng nhập
    /// 3. Giới hạn số lần nhập sai (5 lần)
    /// 4. Khởi tạo Database Context và Services
    /// 5. Mở MainWindow sau khi login thành công
    /// 6. Cho phép mở RegisterWindow để đăng ký
    /// 
    /// QUAN TRỌNG:
    /// - LoginWindow khởi tạo tất cả Services và truyền cho MainWindow
    /// - Phải catch exception khi connect database
    /// </summary>
    public partial class LoginWindow : Window
    {
        // ==================== DATABASE CONTEXT & REPOSITORIES ====================
        // DbContext: Kết nối tới SQL Server database
        private readonly MusicPlayerDbContext _context;
        
        // Repositories: Tầng truy cập dữ liệu (Data Access Layer)
        // Mỗi Repository chịu trách nhiệm CRUD cho 1 bảng
        private readonly UserRepository _userRepo;      // Quản lý Users table
        private readonly SongRepository _songRepo;       // Quản lý Songs table
        private readonly PlaylistRepo _playlistRepo;     // Quản lý Playlists & PlaylistSongs tables

        // ==================== SERVICES (BUSINESS LOGIC LAYER) ====================
        // Services: Tầng logic nghiệp vụ, xử lý các nghiệp vụ phức tạp
        // MainWindow sẽ dùng các Services này, KHÔNG trực tiếp dùng Repositories
        private readonly UserService _userService;           // Login, Register, GetByEmail
        private readonly MusicService _musicService;         // CRUD songs, add/remove favorites
        private readonly PlaylistService _playlistService;   // CRUD playlists, add/remove songs
        
        // ==================== LOGIN ATTEMPT TRACKING ====================
        // Đếm số lần nhập sai mật khẩu để lock account tạm thời
        // static để giữ giá trị khi mở lại LoginWindow
        private static int WrongPasswordCount = 0;
        private const int MaxWrongAttempts = 5;  // Tối đa 5 lần nhập sai
        // ==================== CONSTRUCTOR MẶC ĐỊNH ====================
        /// <summary>
        /// Constructor khởi tạo LoginWindow
        /// 
        /// QUY TRÌNH KHỞI TẠO (3-TIER ARCHITECTURE):
        /// 1. Tạo DbContext (kết nối database)
        /// 2. Tạo Repositories (Data Access Layer)
        /// 3. Tạo Services (Business Logic Layer) - truyền Repositories vào
        /// 
        /// TẠI SAO PHẢI TRY-CATCH:
        /// - Connection string có thể sai
        /// - SQL Server có thể chưa chạy
        /// - Database có thể chưa tồn tại
        /// - Nếu không connect được, app KHÔNG THỂ chạy -> phải Shutdown
        /// 
        /// KIẾN TRÚC 3 TẦNG:
        /// UI (LoginWindow) -> Services (BLL) -> Repositories (DAL) -> Database
        /// </summary>
        public LoginWindow()
        {
            InitializeComponent();
            try
            {
                // ===== BƯỚC 1: Tạo DbContext (kết nối database) =====
                // DbContext đọc connection string từ appsettings.json
                _context = new MusicPlayerDbContext();

                // ===== BƯỚC 2: Tạo Repositories (DAL - Data Access Layer) =====
                // Mỗi Repository cần DbContext để truy cập database
                _userRepo = new UserRepository(_context);
                _songRepo = new SongRepository(_context);
                _playlistRepo = new PlaylistRepo(_context);

                // ===== BƯỚC 3: Tạo Services (BLL - Business Logic Layer) =====
                // Services nhận Repositories làm dependency
                // MainWindow sẽ nhận các Services này để dùng
                _userService = new UserService(_userRepo);
                _musicService = new MusicService(_songRepo);
                _playlistService = new PlaylistService(_playlistRepo);
            }
            catch (Exception ex)
            {
                // Nếu không kết nối được database -> app không thể chạy
                MessageBox.Show($"Can not connect Database.\n Error: {ex.Message}",
                                "Connection", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Shutdown toàn bộ app vì không có database thì không làm gì được
                Application.Current.Shutdown();
            }
        }

        // ==================== CONSTRUCTOR VỚI THAM SỐ ====================
        /// <summary>
        /// Constructor overload để pre-fill email và password
        /// 
        /// SỬ DỤNG KHI:
        /// - Quay lại từ RegisterWindow (sau khi đăng ký thành công)
        /// - Muốn tự động điền email/password cho testing
        /// 
        /// : this() - Gọi constructor mặc định trước để khởi tạo Services
        /// </summary>
        public LoginWindow(String email, String password):this()
        {
            // Pre-fill email và password vào textbox
            EmailText.Text = email;
            PasswordText.Password = password;
        }

        // ==================== XỬ LÝ ĐĂNG NHẬP ====================
        /// <summary>
        /// Event handler khi user click nút "Login"
        /// 
        /// QUY TRÌNH VALIDATE:
        /// 1. Kiểm tra đã nhập sai quá 5 lần chưa
        /// 2. Kiểm tra email có tồn tại không
        /// 3. Kiểm tra password có đúng không
        /// 4. Nếu OK: Mở MainWindow và đóng LoginWindow
        /// 
        /// BẢO MẬT:
        /// - Giới hạn 5 lần nhập sai (brute force protection)
        /// - Validate email trước password (tránh leak info)
        /// - Password được hash trong database
        /// </summary>
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            // ===== BƯỚC 1: Lấy input từ form =====
            var email = EmailText.Text.Trim();
            var pass = PasswordText.Password.Trim();

            // ===== BƯỚC 2: Kiểm tra đã lock account chưa =====
            // Nếu nhập sai >= 5 lần, không cho login nữa
            if (WrongPasswordCount >= MaxWrongAttempts)
            {
                MessageBox.Show("You have entered the wrong password too many times.\nPlease restart the application to try again.",
                                "Account Locked",
                                MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            // ===== BƯỚC 3: Kiểm tra email có tồn tại không =====
            // Validate email TRƯỚC để không leak thông tin password
            var userByEmail = _userService.GetByEmail(email);
            if (userByEmail == null)
            {
                MessageBox.Show("Email does not exist!",
                                "Login Error",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // ===== BƯỚC 4: Validate email + password =====
            // UserService.Login() sẽ so sánh password hash
            var user = _userService.Login(email, pass);

            if (user == null)
            {
                // ===== BƯỚC 5: Xử lý sai password =====
                WrongPasswordCount++; // Tăng số lần sai

                int remain = MaxWrongAttempts - WrongPasswordCount;

                if (remain > 0)
                {
                    // Còn attempt -> cho thử lại
                    MessageBox.Show($"Incorrect password!\nYou have {remain} attempts left.",
                                    "Invalid Password",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    // Hết attempt -> lock account
                    MessageBox.Show("Too many incorrect attempts!\nPlease restart the application to try again.",
                                    "Account Locked",
                                    MessageBoxButton.OK, MessageBoxImage.Stop);
                }

                return;
            }

            // ===== BƯỚC 6: LOGIN THÀNH CÔNG =====
            WrongPasswordCount = 0; // Reset counter

            // Tạo MainWindow và TRUYỀN 3 THAM SỐ:
            // 1. user: User object để biết ai đang login
            // 2. _musicService: Service để quản lý bài hát
            // 3. _playlistService: Service để quản lý playlist
            var main = new MainWindow(user, _musicService, _playlistService);
            main.Show(); // Hiển thị MainWindow

            this.Close(); // Đóng LoginWindow (không cần nữa)
        }

        // ==================== MỞ FORM ĐĂNG KÝ ====================
        /// <summary>
        /// Event handler khi user click link "Register" để đăng ký tài khoản mới
        /// 
        /// LÝ DO CẦN TRUYỀN _userService:
        /// RegisterWindow cần UserService để:
        /// - Kiểm tra email đã tồn tại chưa
        /// - Tạo user mới trong database
        /// - Validate input (email format, password strength)
        /// 
        /// LƯU Ý:
        /// Dùng ShowDialog() thay vì Show() để BLOCK LoginWindow
        /// User phải hoàn tất đăng ký hoặc cancel trước khi quay lại login
        /// </summary>
        private void OpenRegister_Click(object sender, RoutedEventArgs e)
        {
            // Tạo RegisterWindow và truyền UserService để xử lý đăng ký
            var reg = new RegisterWindow(_userService);
            reg.ShowDialog(); // Modal dialog - block LoginWindow cho đến khi đóng RegisterWindow
        }

        // ==================== ĐÓNG ỨNG DỤNG ====================
        /// <summary>
        /// Event handler khi user click nút "Close" hoặc nút X trên title bar
        /// 
        /// LƯU Ý:
        /// Đây là LoginWindow, nếu user đóng thì thoát ứng dụng luôn
        /// Không có cửa sổ nào khác đang mở nên App sẽ tự động shutdown
        /// </summary>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close(); // Đóng LoginWindow -> App sẽ tự động thoát
        }

        // ==================== WINDOW LOADED EVENT ====================
        /// <summary>
        /// Event khi LoginWindow đã load xong UI
        /// 
        /// HIỆN TẠI: Để trống vì không cần xử lý gì đặc biệt
        /// 
        /// CÓ THỂ DÙNG ĐỂ:
        /// - Pre-fill email nếu có saved credentials
        /// - Check database connection khi mở app
        /// - Load app settings
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Để trống - không có logic đặc biệt khi load
        }
    }
}
