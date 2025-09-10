// SettingsView.xaml.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
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

        public void ClearStatus()
        {
            StatusTextBlock.Text = "";
        }

        private void LoadSettingsToUI()
        {
            var settings = Settings.Instance;
            ApiKeyTextBox.Text = settings.ApiKey ?? string.Empty;
            AnthropicApiKeyTextBox.Text = settings.AnthropicApiKey ?? string.Empty;
            DeviantArtClientIdTextBox.Text = settings.DeviantArtClientId ?? string.Empty;
            DeviantArtClientSecretPasswordBox.Password = settings.DeviantArtClientSecret ?? string.Empty;
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
            
            // Load random prompt settings JSON
            LoadRandomPromptSettingsToUI();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // First try to save random prompt settings
            if (!TryParseAndSaveRandomPromptSettings(out string errorMessage))
            {
                StatusTextBlock.Text = errorMessage;
                StatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
                return;
            }
            
            var settings = Settings.Instance;
            var previousHours = settings.AutoGenerateHours;
            
            settings.ApiKey = ApiKeyTextBox.Text;
            settings.AnthropicApiKey = AnthropicApiKeyTextBox.Text;
            settings.DeviantArtClientId = DeviantArtClientIdTextBox.Text;
            settings.DeviantArtClientSecret = DeviantArtClientSecretPasswordBox.Password;
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
            // If hours changed and auto-generate is enabled, restart to recalculate next generation time
            if (settings.AutoGenerateEnabled && settings.AutoGenerateHours > 0 && 
                Math.Abs(previousHours - settings.AutoGenerateHours) > 0.001)
            {
                AutoGenerateService.Instance.Start(settings.AutoGenerateHours);
            }
            else
            {
                AutoGenerateService.Instance.RefreshFromSettings();
            }
            
            StatusTextBlock.Text = "Settings saved successfully!";
            StatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LimeGreen);
            
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


        private void ClearTokensButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = Settings.Instance;
            settings.DeviantArtAccessToken = null;
            settings.DeviantArtRefreshToken = null;
            settings.DeviantArtTokenExpiry = null;
            settings.Save();
            
            StatusTextBlock.Text = "DeviantArt tokens cleared successfully!";
        }
        
        private void LoadRandomPromptSettingsToUI()
        {
            var settings = Settings.Instance;
            var randomPrompts = settings.RandomPrompts;
            
            // Serialize the RandomPromptsSettings to JSON
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            string json = JsonSerializer.Serialize(randomPrompts, options);
            JsonTextBox.Text = json;
        }
        
        private bool TryParseAndSaveRandomPromptSettings(out string errorMessage)
        {
            errorMessage = "";
            
            try
            {
                string jsonText = JsonTextBox.Text;
                if (string.IsNullOrWhiteSpace(jsonText))
                {
                    errorMessage = "JSON cannot be empty";
                    return false;
                }
                
                var newSettings = JsonSerializer.Deserialize<RandomPromptsSettings>(jsonText);
                
                if (newSettings == null)
                {
                    errorMessage = "Failed to deserialize JSON";
                    return false;
                }
                
                // Validate that all required properties exist and are not null
                if (newSettings.Categories == null)
                {
                    errorMessage = "Categories array is required";
                    return false;
                }
                
                if (newSettings.Styles == null)
                {
                    errorMessage = "Styles array is required";
                    return false;
                }
                
                if (newSettings.Moods == null)
                {
                    errorMessage = "Moods array is required";
                    return false;
                }
                
                // Update the settings with the parsed JSON
                var settings = Settings.Instance;
                settings.RandomPrompts.Categories.Clear();
                settings.RandomPrompts.Categories.AddRange(newSettings.Categories.Where(c => !string.IsNullOrWhiteSpace(c)));
                
                settings.RandomPrompts.Styles.Clear();
                settings.RandomPrompts.Styles.AddRange(newSettings.Styles.Where(s => !string.IsNullOrWhiteSpace(s)));
                
                settings.RandomPrompts.Moods.Clear();
                settings.RandomPrompts.Moods.AddRange(newSettings.Moods.Where(m => !string.IsNullOrWhiteSpace(m)));
                
                return true;
            }
            catch (JsonException ex)
            {
                errorMessage = $"Invalid JSON: {ex.Message}";
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error parsing JSON: {ex.Message}";
                return false;
            }
        }
        
        private void RestoreDefaultsButton_Click(object sender, RoutedEventArgs e)
        {
            // Create a new RandomPromptsSettings to get the default values
            var defaultSettings = new RandomPromptsSettings();
            
            // Serialize to JSON and display
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            string json = JsonSerializer.Serialize(defaultSettings, options);
            JsonTextBox.Text = json;
            
            StatusTextBlock.Text = "Default settings restored. Click 'Save Settings' to apply.";
            StatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LimeGreen);
        }
    }
}