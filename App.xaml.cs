// App.xaml.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WallTrek.Services;
using WallTrek.Utilities;
using System.Windows.Input;
using H.NotifyIcon;
using System.Runtime.InteropServices;

namespace WallTrek
{
    public partial class App : Application
    {
        private MainWindow? _window;
        private TaskbarIcon? trayIcon;
        
        public ICommand ShowMainWindowCommand { get; private set; }

        public App()
        {
            this.InitializeComponent();
            
            ShowMainWindowCommand = new RelayCommand(ShowMainWindow);
        }

        private void InitializeTrayIcon()
        {
            // Create tray context menu
            var contextMenu = new MenuFlyout();

            var showItem = new MenuFlyoutItem 
            { 
                Text = "Show WallTrek",
                Command = ShowMainWindowCommand
            };
            contextMenu.Items.Add(showItem);

            contextMenu.Items.Add(new MenuFlyoutSeparator());

            var quitItem = new MenuFlyoutItem 
            { 
                Text = "Quit",
                Command = new RelayCommand(QuitApplication)
            };
            contextMenu.Items.Add(quitItem);

            // Create tray icon
            trayIcon = new TaskbarIcon
            {
                ToolTipText = "WallTrek - AI Wallpaper Generator",
                ContextFlyout = contextMenu,
                IconSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///assets/walltrek.ico"))
            };

            // Handle left click to show window
            trayIcon.LeftClickCommand = ShowMainWindowCommand;
            
            // Handle double click to show window as well
            trayIcon.DoubleClickCommand = ShowMainWindowCommand;

            trayIcon.ForceCreate();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {             
            Settings.Instance.Load();
            InitializeTrayIcon();

            _window = new MainWindow();
            
            // Show window normally on first run, otherwise start minimized to tray
            if (Settings.Instance.IsFirstRun)
            {
                _window.Activate();
                Settings.Instance.IsFirstRun = false;
                Settings.Instance.Save();
            }
            
            // Connect auto-generate events
            AutoGenerateService.Instance.AutoGenerateTriggered += OnAutoGenerateTriggered;
            
            // Initialize auto-generate service based on current settings
            AutoGenerateService.Instance.RefreshFromSettings();
        }
        
        private void OnAutoGenerateTriggered(object? sender, EventArgs e)
        {
            if (_window != null)
            {
                _window.DispatcherQueue.TryEnqueue(() => _window.TriggerAutoGenerate());
            }
        }
        
        private void ShowMainWindow()
        {
            if (_window != null)
            {
                // Show the window if it's hidden
                _window.AppWindow.Show();
                
                // Activate and bring to front
                _window.Activate();
                
                // Use Win32 API to ensure window comes to foreground
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);
                Win32Helper.BringWindowToFront(hwnd);
            }
        }
        
        private void QuitApplication()
        {
            AutoGenerateService.Instance.Stop();
            trayIcon?.Dispose();
            _window?.Close();
            Exit();
        }

        public void ShowBalloonTip(string title, string message)
        {
            trayIcon?.ShowNotification(title, message);
        }

        public MainWindow? GetMainWindow()
        {
            return _window;
        }
    }
}