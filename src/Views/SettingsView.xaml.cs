// SettingsView.xaml.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using WallTrek.Services;
using Windows.Storage;
using Windows.Storage.Pickers;

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
            OutputDirectoryTextBox.Text = settings.OutputDirectory;
            AutoGenerateCheckBox.IsChecked = settings.AutoGenerateEnabled;
            AutoGenerateHoursNumberBox.Value = settings.AutoGenerateHours > 0 ? settings.AutoGenerateHours : 6.0;
            MinimizeToTrayCheckBox.IsChecked = settings.MinimizeToTray;
            
            // Set auto-generate source dropdown
            foreach (ComboBoxItem item in AutoGenerateSourceComboBox.Items)
            {
                if (item.Tag?.ToString() == settings.AutoGenerateSource)
                {
                    AutoGenerateSourceComboBox.SelectedItem = item;
                    break;
                }
            }
            
            // Default to first item if nothing was selected
            if (AutoGenerateSourceComboBox.SelectedItem == null)
            {
                AutoGenerateSourceComboBox.SelectedIndex = 0;
            }
            
            // Show/hide auto-generate options based on checkbox state
            UpdateAutoGenerateOptionsVisibility();
            
            // Load startup setting from both settings and registry to ensure sync
            RunOnStartupCheckBox.IsChecked = StartupManager.IsStartupEnabled();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = Settings.Instance;
            settings.ApiKey = ApiKeyTextBox.Text;
            settings.OutputDirectory = OutputDirectoryTextBox.Text;
            settings.AutoGenerateEnabled = AutoGenerateCheckBox.IsChecked ?? false;
            settings.AutoGenerateHours = AutoGenerateHoursNumberBox.Value;
            settings.MinimizeToTray = MinimizeToTrayCheckBox.IsChecked ?? true;
            
            // Save auto-generate source
            if (AutoGenerateSourceComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                settings.AutoGenerateSource = selectedItem.Tag?.ToString() ?? "current";
            }
            
            // Handle startup setting - only update registry, no need to store in settings
            var runOnStartup = RunOnStartupCheckBox.IsChecked ?? false;
            StartupManager.SetStartupEnabled(runOnStartup);

            settings.Save();
            
            // Refresh auto-generate service based on new settings
            AutoGenerateService.Instance.RefreshFromSettings();
            
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

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add("*");

            // Get the current window handle for the picker
            var window = ((App)Application.Current).GetMainWindow();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                OutputDirectoryTextBox.Text = folder.Path;
            }
        }

        private void AutoGenerateCheckBox_CheckChanged(object sender, RoutedEventArgs e)
        {
            UpdateAutoGenerateOptionsVisibility();
        }

        private void UpdateAutoGenerateOptionsVisibility()
        {
            AutoGenerateOptionsPanel.Visibility = AutoGenerateCheckBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}