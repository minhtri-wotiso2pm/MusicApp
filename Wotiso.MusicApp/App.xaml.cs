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
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private void LogDebug(string message)
        {
            Debug.WriteLine($"[App] {DateTime.Now:HH:mm:ss.fff} - {message}");
            try
            {
                File.AppendAllText("D:\\musicapp_debug.log", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [APP] - {message}\n");
            }
            catch { }
        }

        public App()
        {
            // Catch all unhandled exceptions
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            
            LogDebug("====== APPLICATION STARTED ======");
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogDebug($"!!!!! UNHANDLED DISPATCHER EXCEPTION !!!!!");
            LogDebug($"Exception: {e.Exception.Message}");
            LogDebug($"Stack Trace: {e.Exception.StackTrace}");
            LogDebug($"Inner Exception: {e.Exception.InnerException?.Message}");

            MessageBox.Show($"UNHANDLED EXCEPTION:\n\n{e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}\n\nCheck log: D:\\musicapp_debug.log",
                "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            
            // Prevent app from crashing
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            LogDebug($"!!!!! UNHANDLED DOMAIN EXCEPTION !!!!!");
            LogDebug($"Exception: {ex?.Message}");
            LogDebug($"Stack Trace: {ex?.StackTrace}");
            LogDebug($"Is Terminating: {e.IsTerminating}");

            MessageBox.Show($"FATAL DOMAIN EXCEPTION:\n\n{ex?.Message}\n\nCheck log: D:\\musicapp_debug.log",
                "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                LogDebug("===== OnStartup START =====");
                base.OnStartup(e);
                LogDebug("base.OnStartup() completed");

                LogDebug("Creating LoginWindow...");
                var loginWindow = new Views.LoginWindow();
                LogDebug("LoginWindow created successfully");
                
                LogDebug("Showing LoginWindow...");
                loginWindow.Show();
                LogDebug("LoginWindow shown successfully");
                
                LogDebug("===== OnStartup COMPLETED =====");
            }
            catch (Exception ex)
            {
                LogDebug($"!!!!! EXCEPTION in OnStartup: {ex.Message}");
                LogDebug($"Stack Trace: {ex.StackTrace}");
                
                MessageBox.Show($"ERROR in App.OnStartup:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            LogDebug("====== APPLICATION EXITING ======");
            base.OnExit(e);
        }
    }
}
