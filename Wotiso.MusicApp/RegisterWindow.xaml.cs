using System.Text.RegularExpressions;
using System.Windows;
using Wotiso.MusicApp.BLL.Services;

namespace Wotiso.MusicApp.Views
{
    /// <summary>
    /// RegisterWindow - Cửa sổ đăng ký tài khoản mới
    /// 
    /// CHỨC NĂNG:
    /// 1. Cho phép user tạo tài khoản mới với Name, Email, Password
    /// 2. Validate input theo các rule nghiệp vụ
    /// 3. Tạo user trong database qua UserService
    /// 4. Tự động login sau khi đăng ký thành công
    /// 
    /// KIẾN TRÚC:
    /// - UI Layer: RegisterWindow.xaml (XAML) + RegisterWindow.xaml.cs (Code-behind)
    /// - Business Layer: UserService (xử lý đăng ký)
    /// - Data Layer: UserRepository -> Database
    /// 
    /// FLOW:
    /// User nhập thông tin -> Validate -> UserService.Register() -> LoginWindow
    /// </summary>
    public partial class RegisterWindow : Window
    {
        // ==================== BIẾN SERVICE ====================
        /// <summary>
        /// UserService - Service để xử lý đăng ký user
        /// 
        /// NHIỆM VỤ CỦA SERVICE:
        /// 1. Register(name, email, pass): Tạo user mới trong database
        /// 2. GetByEmail(email): Kiểm tra email đã tồn tại chưa
        /// 3. Hash password trước khi lưu (bảo mật)
        /// 
        /// READONLY:
        /// Không cho thay đổi sau khi khởi tạo (immutability)
        /// </summary>
        private readonly UserService _service;

        // ==================== CONSTRUCTOR 1 (ĐƯỢC GỌI TỪ LOGINWINDOW) ====================
        /// <summary>
        /// Constructor chính - Nhận UserService từ LoginWindow
        /// 
        /// TẠI SAO CẦN TRUYỀN SERVICE VÀO:
        /// - LoginWindow đã khởi tạo UserService với DbContext
        /// - Tái sử dụng service thay vì tạo mới (tiết kiệm tài nguyên)
        /// - Đảm bảo cùng 1 database connection context
        /// 
        /// ĐƯỢC GỌI BỞI:
        /// LoginWindow.OpenRegister_Click() -> new RegisterWindow(_userService)
        /// </summary>
        public RegisterWindow(UserService userService)
        {
            InitializeComponent(); // Load XAML UI
            _service = userService; // Lưu service để dùng trong Register_Click
        }

        // ==================== CONSTRUCTOR 2 (FALLBACK) ====================
        /// <summary>
        /// Constructor mặc định - Không nhận tham số
        /// 
        /// LƯU Ý:
        /// - Được giữ lại để tương thích với XAML designer
        /// - KHÔNG DÙNG trong production vì _service sẽ null
        /// - Chỉ để VS designer load được preview
        /// 
        /// CẢN BÁO:
        /// Nếu gọi constructor này và click Register sẽ bị NullReferenceException
        /// </summary>
        public RegisterWindow()
        {
            InitializeComponent();
            // _service = null (không khởi tạo)
        }

        // ==================== XỬ LÝ ĐĂNG KÝ ====================
        /// <summary>
        /// Event handler khi user click nút "Register"
        /// 
        /// QUY TRÌNH VALIDATE (theo thứ tự):
        /// 1. Name: Độ dài 4-30 ký tự
        /// 2. Email: Format hợp lệ (regex)
        /// 3. Password: Tối thiểu 8 ký tự
        /// 4. Confirm Password: Phải trùng với Password
        /// 5. Email chưa tồn tại trong database
        /// 
        /// FLOW THÀNH CÔNG:
        /// Register OK -> Tự động mở LoginWindow với email/pass đã nhập -> Đóng RegisterWindow
        /// 
        /// BẢO MẬT:
        /// - Password được hash bởi UserService trước khi lưu database
        /// - Không log password ra file/console
        /// </summary>
        private void Register_Click(object sender, RoutedEventArgs e)
        {
            // ===== BƯỚC 1: Lấy input từ form =====
            // Trim() để loại bỏ khoảng trắng đầu/cuối
            var name = NameText.Text.Trim();
            var email = EmailText.Text.Trim();
            var pass = PasswordText.Password.Trim();
            var confirm = ConfirmPasswordText.Password.Trim();

            // ===== BƯỚC 2: Validate Name =====
            // Rule nghiệp vụ: Name phải 4-30 ký tự
            // Lý do: Quá ngắn = không rõ ràng, quá dài = khó hiển thị UI
            if (name.Length < 4 || name.Length > 30)
            {
                MessageBox.Show("Name must be between 4 and 30 characters.",
                                "Invalid Name",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return; // Dừng lại, không validate tiếp
            }

            // ===== BƯỚC 3: Validate Email Format =====
            // Dùng regex để check email có đúng format không
            if (!IsValidEmail(email))
            {
                MessageBox.Show("Invalid email format!",
                                "Invalid Email",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // ===== BƯỚC 4: Validate Password Length =====
            // Rule bảo mật: Password tối thiểu 8 ký tự
            if (pass.Length < 8)
            {
                MessageBox.Show("Password must be at least 8 characters long.",
                                "Invalid Password",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // ===== BƯỚC 5: Validate Password Confirmation =====
            // Đảm bảo user không nhập nhầm password
            if (pass != confirm)
            {
                MessageBox.Show("Passwords do not match!",
                                "Password Error",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // ===== BƯỚC 6: Gọi UserService.Register() =====
            // UserService sẽ:
            // 1. Check email đã tồn tại chưa
            // 2. Hash password
            // 3. Tạo User entity
            // 4. Lưu vào database
            // Return: true = thành công, false = email đã tồn tại
            if (_service.Register(name, email, pass))
            {
                // ===== ĐĂNG KÝ THÀNH CÔNG =====
                // Tạo LoginWindow và TỰ ĐỘNG ĐIỀN SẴN email + password
                // User chỉ cần click "Login" để vào app
                var login = new LoginWindow(email, pass);
                login.Show(); // Hiển thị LoginWindow

                this.Close(); // Đóng RegisterWindow (không cần nữa)
            }
            else
            {
                // ===== EMAIL ĐÃ TỒN TẠI =====
                // UserService.Register() return false
                MessageBox.Show("This email is already registered!",
                                "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== VALIDATE EMAIL FORMAT ====================
        /// <summary>
        /// Kiểm tra email có đúng format không bằng Regex
        /// 
        /// REGEX PATTERN:
        /// ^[^@\s]+@[^@\s]+\.[^@\s]+$
        /// 
        /// GIẢI THÍCH:
        /// - ^: Bắt đầu chuỗi
        /// - [^@\s]+: 1 hoặc nhiều ký tự KHÔNG phải @ và khoảng trắng (local part)
        /// - @: Bắt buộc có ký tự @
        /// - [^@\s]+: 1 hoặc nhiều ký tự KHÔNG phải @ và khoảng trắng (domain)
        /// - \.: Bắt buộc có dấu chấm
        /// - [^@\s]+: 1 hoặc nhiều ký tự KHÔNG phải @ và khoảng trắng (TLD)
        /// - $: Kết thúc chuỗi
        /// 
        /// VÍ DỤ HỢP LỆ:
        /// - user@example.com
        /// - test.name@domain.co.uk
        /// - 123@test.org
        /// 
        /// VÍ DỤ KHÔNG HỢP LỆ:
        /// - user@example (thiếu TLD)
        /// - @example.com (thiếu local part)
        /// - user example@test.com (có khoảng trắng)
        /// - user@@example.com (2 ký tự @)
        /// 
        /// THAM SỐ:
        /// - email: Chuỗi cần validate
        /// 
        /// RETURN:
        /// - true: Email hợp lệ
        /// - false: Email không hợp lệ
        /// 
        /// LƯU Ý:
        /// - Regex này KHÔNG validate email thực sự tồn tại (cần SMTP verify)
        /// - Chỉ check FORMAT có đúng chuẩn không
        /// - IgnoreCase: Không phân biệt hoa thường
        /// </summary>
        private bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase);
        }

        // ==================== ĐÓNG CỬA SỔ ĐĂNG KÝ ====================
        /// <summary>
        /// Event handler khi user click nút "Close" hoặc nút X
        /// 
        /// HÀNH VI:
        /// Đóng RegisterWindow và quay lại LoginWindow (vì dùng ShowDialog())
        /// 
        /// LƯU Ý:
        /// - RegisterWindow được mở bằng ShowDialog() từ LoginWindow
        /// - ShowDialog() = Modal dialog, block LoginWindow cho đến khi đóng
        /// - Khi Close() -> LoginWindow sẽ được unblock và user có thể login
        /// </summary>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close(); // Đóng RegisterWindow -> Quay lại LoginWindow
        }
    }
}
