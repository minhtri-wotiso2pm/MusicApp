using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace Wotiso.MusicApp
{
    /// <summary>
    /// ==================== APP.XAML.CS - ENTRY POINT CỦA ỨNG DỤNG ====================
    /// File này là nơi khởi động đầu tiên của ứng dụng WPF
    /// 
    /// NHIỆM VỤ CHÍNH:
    /// 1. Catch tất cả exceptions chưa được xử lý (Unhandled Exceptions)
    /// 2. Log mọi hoạt động của app để debug
    /// 3. Khởi tạo và hiển thị LoginWindow
    /// 4. Ngăn app crash khi có lỗi bất ngờ
    /// 
    /// QUAN TRỌNG: 
    /// - Không được để exception nào thoát ra ngoài mà không được log
    /// - Phải có exception handling ở đây vì đây là tầng cao nhất
    /// </summary>
    public partial class App : System.Windows.Application
    {
        // ==================== LOGGING ====================
        /// <summary>
        /// Ghi log cho toàn bộ application
        /// Tương tự LogDebug trong MainWindow nhưng prefix là [App]
        /// </summary>
        private void LogDebug(string message)
        {
            // Log ra Visual Studio Debug Console
            Debug.WriteLine($"[App] {DateTime.Now:HH:mm:ss.fff} - {message}");
            try
            {
                // Ghi vào file D:\musicapp_debug.log để xem lại
                File.AppendAllText("D:\\musicapp_debug.log", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [APP] - {message}\n");
            }
            catch { } // Không được crash chỉ vì ghi log lỗi
        }

        // ==================== CONSTRUCTOR ====================
        /// <summary>
        /// Constructor của App - chạy ĐẦU TIÊN khi app khởi động
        /// 
        /// NHIỆM VỤ:
        /// 1. Đăng ký exception handlers để catch mọi lỗi
        /// 2. Log thời điểm app bắt đầu
        /// 
        /// 2 LOẠI EXCEPTION HANDLER:
        /// - DispatcherUnhandledException: Lỗi trên UI thread (thường gặp nhất)
        /// - CurrentDomain.UnhandledException: Lỗi trên background threads
        /// </summary>
        public App()
        {
            // Đăng ký handler cho lỗi trên UI thread (WPF Dispatcher)
            // Ví dụ: NullReferenceException khi click button, binding error, etc.
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            
            // Đăng ký handler cho lỗi trên các thread khác (background tasks)
            // Ví dụ: Lỗi trong Task.Run(), Thread.Start(), etc.
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            
            LogDebug("====== APPLICATION STARTED ======");
        }

        // ==================== UI THREAD EXCEPTION HANDLER ====================
        /// <summary>
        /// Bắt TẤT CẢ exception xảy ra trên UI thread (Dispatcher thread)
        /// 
        /// KHI NÀO CHẠY:
        /// - Click button mà code bên trong throw exception
        /// - Binding error
        /// - Event handler throw exception
        /// - Bất kỳ code nào chạy trên UI thread mà throw exception
        /// 
        /// QUAN TRỌNG: e.Handled = true để NGĂN APP CRASH
        /// Nếu không set Handled = true, app sẽ crash ngay lập tức
        /// </summary>
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Log chi tiết exception để debug
            LogDebug($"!!!!! UNHANDLED DISPATCHER EXCEPTION !!!!!");
            LogDebug($"Exception: {e.Exception.Message}");
            LogDebug($"Stack Trace: {e.Exception.StackTrace}");
            LogDebug($"Inner Exception: {e.Exception.InnerException?.Message}");

            // Hiển thị lỗi cho user (trong production nên hiển thị thân thiện hơn)
            MessageBox.Show($"UNHANDLED EXCEPTION:\n\n{e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}\n\nCheck log: D:\\musicapp_debug.log",
                "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            
            // ===== QUAN TRỌNG =====
            // Set Handled = true để app KHÔNG CRASH
            // App sẽ tiếp tục chạy sau khi user đóng MessageBox
            e.Handled = true;
        }

        // ==================== BACKGROUND THREAD EXCEPTION HANDLER ====================
        /// <summary>
        /// Bắt exception xảy ra trên các thread KHÔNG PHẢI UI thread
        /// 
        /// KHI NÀO CHẠY:
        /// - Exception trong Task.Run()
        /// - Exception trong Thread.Start()
        /// - Exception trong ThreadPool
        /// 
        /// LƯU Ý: Không thể Handled = true ở đây
        /// Nếu IsTerminating = true, app sẽ crash KHÔNG THỂ NGĂN
        /// </summary>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            LogDebug($"!!!!! UNHANDLED DOMAIN EXCEPTION !!!!!");
            LogDebug($"Exception: {ex?.Message}");
            LogDebug($"Stack Trace: {ex?.StackTrace}");
            LogDebug($"Is Terminating: {e.IsTerminating}"); // true = app sẽ crash

            MessageBox.Show($"FATAL DOMAIN EXCEPTION:\n\n{ex?.Message}\n\nCheck log: D:\\musicapp_debug.log",
                "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // ==================== APP STARTUP ====================
        /// <summary>
        /// OnStartup - Method chạy SAU constructor, là nơi khởi tạo window đầu tiên
        /// 
        /// QUY TRÌNH:
        /// 1. Gọi base.OnStartup() - bắt buộc phải có
        /// 2. Tạo LoginWindow
        /// 3. Hiển thị LoginWindow bằng Show()
        /// 
        /// TẠI SAO KHÔNG DÙNG StartupUri trong App.xaml:
        /// - Vì cần truyền parameters vào MainWindow
        /// - Cần LoginWindow chạy trước
        /// - Cần log từng bước để debug
        /// 
        /// LƯU Ý: 
        /// - Phải wrap trong try-catch vì đây là điểm khởi đầu
        /// - Nếu lỗi ở đây, app sẽ không khởi động được
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                LogDebug("===== OnStartup START =====");
                
                // Gọi OnStartup của class cha (Application)
                // Bắt buộc phải có, nếu không app sẽ không chạy đúng
                base.OnStartup(e);
                LogDebug("base.OnStartup() completed");

                // Tạo LoginWindow - window đầu tiên user nhìn thấy
                LogDebug("Creating LoginWindow...");
                var loginWindow = new Views.LoginWindow();
                LogDebug("LoginWindow created successfully");
                
                // Show LoginWindow - bắt đầu UI flow
                // Sau khi login thành công, LoginWindow sẽ tạo MainWindow
                LogDebug("Showing LoginWindow...");
                loginWindow.Show();
                LogDebug("LoginWindow shown successfully");
                
                LogDebug("===== OnStartup COMPLETED =====");
            }
            catch (Exception ex)
            {
                // Nếu có lỗi trong quá trình startup
                LogDebug($"!!!!! EXCEPTION in OnStartup: {ex.Message}");
                LogDebug($"Stack Trace: {ex.StackTrace}");
                
                // Hiển thị lỗi cho user
                MessageBox.Show($"ERROR in App.OnStartup:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Throw lại exception để app không chạy tiếp (vì đã lỗi từ đầu)
                throw;
            }
        }

        // ==================== APP EXIT ====================
        /// <summary>
        /// OnExit - Method chạy khi app đóng (user tắt hoặc Application.Shutdown())
        /// 
        /// DÙNG ĐỂ:
        /// - Cleanup resources
        /// - Save settings
        /// - Close database connections
        /// - Log thời điểm tắt app
        /// 
        /// Hiện tại chỉ log, có thể thêm cleanup logic sau
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            LogDebug("====== APPLICATION EXITING ======");
            base.OnExit(e);
        }
    }
}
