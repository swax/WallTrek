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
        private MainWindow? m_window;
        private SettingsWindow? settingsWindow;
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
            // Create tray context menu
            var contextMenu = new MenuFlyout();
            
            var showItem = new MenuFlyoutItem { Text = "Show WallTrek" };
            showItem.Click += ShowMenuItem_Click;
            contextMenu.Items.Add(showItem);
            
            var settingsItem = new MenuFlyoutItem { Text = "Settings" };
            settingsItem.Click += SettingsMenuItem_Click;
            contextMenu.Items.Add(settingsItem);
            
            contextMenu.Items.Add(new MenuFlyoutSeparator());
            
            var quitItem = new MenuFlyoutItem { Text = "Quit" };
            quitItem.Click += QuitMenuItem_Click;
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
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            
            // Start minimized to tray - don't show window initially
            // m_window.Activate();
            
            // Connect auto-generate events
            AutoGenerateService.Instance.AutoGenerateTriggered += OnAutoGenerateTriggered;
            
            // Restore auto-generate timer if it was running
            if (Settings.Instance.AutoGenerateEnabled && Settings.Instance.NextAutoGenerateTime.HasValue)
            {
                AutoGenerateService.Instance.StartFromSavedTime();
            }
        }
        
        private void OnAutoGenerateTriggered(object? sender, EventArgs e)
        {
            if (m_window != null)
            {
                m_window.DispatcherQueue.TryEnqueue(() => m_window.TriggerAutoGenerate());
            }
        }
        
        private void ShowMainWindow()
        {
            m_window?.Activate();
        }
        
        private void ShowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShowMainWindow();
        }
        
        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (settingsWindow == null)
            {
                settingsWindow = new SettingsWindow();
                settingsWindow.Closed += (s, e) => settingsWindow = null;
            }
            settingsWindow.Activate();
        }
        
        private void QuitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AutoGenerateService.Instance.Stop();
            trayIcon?.Dispose();
            Exit();
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

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();
    }
}