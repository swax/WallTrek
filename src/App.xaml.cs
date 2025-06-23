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
using System.Windows.Input;
using H.NotifyIcon;

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
            Settings.Instance.Load();
            ShowMainWindowCommand = new RelayCommand(ShowMainWindow);
            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            // Create tray context menu with commands instead of click events
            // The menu shows in the bottom right corner of the screen and not over the icon
            // The experimental/preview version of h.NotifyIcon does support it
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
                IconSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/walltrek.ico"))
            };

            // Handle left click to show window
            trayIcon.LeftClickCommand = ShowMainWindowCommand;
            
            // Handle double click to show window as well
            trayIcon.DoubleClickCommand = ShowMainWindowCommand;

            trayIcon.ForceCreate();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            
            // Start minimized to tray - don't show window initially
            // m_window.Activate();
            
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
            _window?.Activate();
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
    }
    
    // Simple RelayCommand implementation
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();
    }
}