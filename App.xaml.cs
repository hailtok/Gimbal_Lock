using System;
using System.IO;
using System.Windows;

namespace GimbalLockVisualizer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                try
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_appdomain.txt");
                    File.WriteAllText(path, args.ExceptionObject?.ToString() ?? "Unknown exception");
                }
                catch { }
            };

            DispatcherUnhandledException += (s, args) =>
            {
                try
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_dispatcher.txt");
                    File.WriteAllText(path, args.Exception?.ToString() ?? "Unknown dispatcher exception");
                }
                catch { }
                args.Handled = false;
            };

            try
            {
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                var window = new MainWindow();
                MainWindow = window;
                window.Show();
            }
            catch (Exception ex)
            {
                try
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_exception.txt");
                    File.WriteAllText(path, ex.ToString());
                }
                catch { }
                MessageBox.Show(ex.ToString(), "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
