// SettingsView.xaml.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using WallTrek.Services;

namespace WallTrek.Views
{
    public sealed partial class SettingsView : UserControl
    {
        public event EventHandler? NavigateToMain;

        public SettingsView()
        {
            this.InitializeComponent();
            LoadSettingsToUI();
        }

        private void LoadSettingsToUI()
        {
            var settings = Settings.Instance;
            ApiKeyTextBox.Text = settings.ApiKey ?? string.Empty;
            AutoGenerateCheckBox.IsChecked = settings.AutoGenerateEnabled;
            AutoGenerateMinutesNumberBox.Value = settings.AutoGenerateMinutes > 0 ? settings.AutoGenerateMinutes : 60;
            MinimizeToTrayCheckBox.IsChecked = settings.MinimizeToTray;
            
            // Load startup setting from both settings and registry to ensure sync
            RunOnStartupCheckBox.IsChecked = StartupManager.IsStartupEnabled();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = Settings.Instance;
            settings.ApiKey = ApiKeyTextBox.Text;
            settings.AutoGenerateEnabled = AutoGenerateCheckBox.IsChecked ?? false;
            settings.AutoGenerateMinutes = (int)AutoGenerateMinutesNumberBox.Value;
            settings.MinimizeToTray = MinimizeToTrayCheckBox.IsChecked ?? true;
            
            // Handle startup setting - only update registry, no need to store in settings
            var runOnStartup = RunOnStartupCheckBox.IsChecked ?? false;
            StartupManager.SetStartupEnabled(runOnStartup);

            settings.Save();
            StatusTextBlock.Text = "Settings saved successfully!";
            
            // Navigate back to main view after saving
            NavigateToMain?.Invoke(this, EventArgs.Empty);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Reload settings to discard changes
            LoadSettingsToUI();
            StatusTextBlock.Text = "";
            
            // Navigate back to main view
            NavigateToMain?.Invoke(this, EventArgs.Empty);
        }
    }
}