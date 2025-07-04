// MainWindow.xaml.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using WallTrek.Services;
using Windows.UI;

namespace WallTrek
{
    public enum ViewType
    {
        Home,
        Settings,
        History,
        RandomPromptSettings
    }

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
            SettingsViewControl.NavigateToRandomPromptSettings += (s, e) => NavigateToRandomPromptSettings();
            HistoryViewControl.NavigateBack += (s, e) => NavigateToHome();
            HistoryViewControl.CopyPrompt += (s, prompt) => {
                MainViewControl.SetPromptText(prompt);
                NavigateToHome();
            };
            RandomPromptSettingsViewControl.NavigateBack += (s, e) => NavigateToSettings();
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

        private void SetActiveView(ViewType viewType)
        {
            MainViewControl.Visibility = viewType == ViewType.Home ? Visibility.Visible : Visibility.Collapsed;
            SettingsViewControl.Visibility = viewType == ViewType.Settings ? Visibility.Visible : Visibility.Collapsed;
            HistoryViewControl.Visibility = viewType == ViewType.History ? Visibility.Visible : Visibility.Collapsed;
            RandomPromptSettingsViewControl.Visibility = viewType == ViewType.RandomPromptSettings ? Visibility.Visible : Visibility.Collapsed;
        }


        private void NavigateToHome()
        {
            SetActiveView(ViewType.Home);
        }

        private void NavigateToSettings()
        {
            SetActiveView(ViewType.Settings);
            SettingsViewControl.ClearStatus();
        }

        private void NavigateToHistory()
        {
            SetActiveView(ViewType.History);
            HistoryViewControl.RefreshHistory();
        }

        private void NavigateToRandomPromptSettings()
        {
            SetActiveView(ViewType.RandomPromptSettings);
            RandomPromptSettingsViewControl.ClearStatus();
        }
    }
}