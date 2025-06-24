// MainWindow.xaml.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using WallTrek.Services;
using Windows.UI;

namespace WallTrek
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "WallTrek";
            
            // Set window icon
            this.AppWindow.SetIcon("assets/walltrek.ico");
            
            // Remove title bar
            var titleBar = this.AppWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;
            titleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;
            
            // Handle window state changes to minimize to tray based on setting
            this.AppWindow.Changed += AppWindow_Changed;
            
            // Connect view events
            MainViewControl.NavigateToSettings += (s, e) => NavigateToSettings();
            MainViewControl.NavigateToHistory += (s, e) => NavigateToHistory();
            SettingsViewControl.NavigateToMain += (s, e) => NavigateToHome();
            HistoryViewControl.NavigateBack += (s, e) => NavigateToHome();
        }
        
        private void AppWindow_Changed(object? sender, Microsoft.UI.Windowing.AppWindowChangedEventArgs e)
        {
            // Check if the window was minimized and the setting is enabled
            if (e.DidPresenterChange && Settings.Instance.MinimizeToTray)
            {
                if (this.AppWindow.Presenter.Kind == Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped)
                {
                    var overlappedPresenter = this.AppWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
                    if (overlappedPresenter?.State == Microsoft.UI.Windowing.OverlappedPresenterState.Minimized)
                    {
                        // Hide the window instead of showing it minimized
                        this.AppWindow.Hide();
                    }
                }
            }
        }
        
        public void TriggerAutoGenerate()
        {
            // Delegate to the MainView
            MainViewControl.TriggerAutoGenerate();
        }


        private void NavigateToHome()
        {
            MainViewControl.Visibility = Visibility.Visible;
            SettingsViewControl.Visibility = Visibility.Collapsed;
            HistoryViewControl.Visibility = Visibility.Collapsed;
        }

        private void NavigateToSettings()
        {
            MainViewControl.Visibility = Visibility.Collapsed;
            SettingsViewControl.Visibility = Visibility.Visible;
            HistoryViewControl.Visibility = Visibility.Collapsed;
        }

        private void NavigateToHistory()
        {
            MainViewControl.Visibility = Visibility.Collapsed;
            SettingsViewControl.Visibility = Visibility.Collapsed;
            HistoryViewControl.Visibility = Visibility.Visible;
            HistoryViewControl.RefreshHistory();
        }
    }
}