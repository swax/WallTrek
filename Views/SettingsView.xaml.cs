// SettingsView.xaml.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using WallTrek.Services;
using WallTrek.Services.Profiles;
using WallTrek.Services.TextGen;
using WallTrek.Utilities;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace WallTrek.Views
{
    public sealed partial class SettingsView : UserControl
    {
        public event EventHandler? NavigateToMain;

        // The profile / word list whose contents are currently shown in each editor.
        private string? _currentCategoryProfile;
        private string? _currentWordList;

        // Guards the combo SelectionChanged handlers while we populate them in code.
        private bool _suppressProfileEvents;

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
            GoogleApiKeyTextBox.Text = settings.GoogleApiKey ?? string.Empty;
            StabilityApiKeyTextBox.Text = settings.StabilityApiKey ?? string.Empty;
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

            // Load random words on/off + count
            AddRandomWordsCheckBox.IsChecked = settings.AddRandomWords;
            RandomWordCountNumberBox.Value = settings.RandomWordCount;

            // Load the file-based category profiles and word lists into their dropdowns/editors.
            PopulateCategoryProfiles();
            PopulateWordLists();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Flush the open category editor to its file (must be valid JSON).
            if (_currentCategoryProfile != null)
            {
                if (!CategoryProfileService.TryValidate(JsonTextBox.Text, out string catError))
                {
                    ShowStatus(catError, Microsoft.UI.Colors.Red);
                    return;
                }
                CategoryProfileService.WriteText(_currentCategoryProfile, JsonTextBox.Text);
                Settings.Instance.SelectedCategoryProfile = _currentCategoryProfile;
            }

            // Flush the open word-list editor to its file (any text is allowed).
            if (_currentWordList != null)
            {
                RandomWordService.WriteText(_currentWordList, WordListTextBox.Text);
                Settings.Instance.SelectedWordList = _currentWordList;
            }

            var settings = Settings.Instance;
            var previousHours = settings.AutoGenerateHours;

            settings.ApiKey = ApiKeyTextBox.Text;
            settings.AnthropicApiKey = AnthropicApiKeyTextBox.Text;
            settings.GoogleApiKey = GoogleApiKeyTextBox.Text;
            settings.StabilityApiKey = StabilityApiKeyTextBox.Text;
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

            // Save random words settings
            settings.AddRandomWords = AddRandomWordsCheckBox.IsChecked ?? false;
            settings.RandomWordCount = (int)RandomWordCountNumberBox.Value;

            // Handle startup setting - only update registry, no need to store in settings
            var runOnStartup = RunOnStartupCheckBox.IsChecked ?? false;
            StartupManager.SetStartupEnabled(runOnStartup);

            settings.Save();

            // Refresh auto-generate service based on new settings
            if (settings.AutoGenerateEnabled && settings.AutoGenerateHours > 0 &&
                Math.Abs(previousHours - settings.AutoGenerateHours) > 0.001)
            {
                AutoGenerateService.Instance.Start(settings.AutoGenerateHours);
            }
            else
            {
                AutoGenerateService.Instance.RefreshFromSettings();
            }

            ShowStatus("Settings saved successfully!", Microsoft.UI.Colors.LimeGreen);

            // Navigate back to main view after saving
            NavigateToMain?.Invoke(this, EventArgs.Empty);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Reload settings to discard unsaved editor changes
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

        // ---- Category profiles ------------------------------------------------

        private void PopulateCategoryProfiles()
        {
            _suppressProfileEvents = true;
            var profiles = CategoryProfileService.ListProfiles().ToList();
            CategoryProfileComboBox.ItemsSource = profiles;

            var active = profiles.FirstOrDefault(p =>
                            string.Equals(p, Settings.Instance.SelectedCategoryProfile, StringComparison.OrdinalIgnoreCase))
                        ?? profiles.FirstOrDefault();
            CategoryProfileComboBox.SelectedItem = active;
            _suppressProfileEvents = false;

            _currentCategoryProfile = active;
            JsonTextBox.Text = active != null ? CategoryProfileService.LoadText(active) : string.Empty;
        }

        private void CategoryProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressProfileEvents) return;
            if (CategoryProfileComboBox.SelectedItem is not string target) return;
            if (string.Equals(target, _currentCategoryProfile, StringComparison.Ordinal)) return;

            // Auto-save the editor to the previously selected profile (must be valid JSON).
            if (_currentCategoryProfile != null)
            {
                if (!CategoryProfileService.TryValidate(JsonTextBox.Text, out var err))
                {
                    ShowStatus($"Can't switch — fix the JSON first: {err}", Microsoft.UI.Colors.Red);
                    _suppressProfileEvents = true;
                    CategoryProfileComboBox.SelectedItem = _currentCategoryProfile;
                    _suppressProfileEvents = false;
                    return;
                }
                CategoryProfileService.WriteText(_currentCategoryProfile, JsonTextBox.Text);
            }

            _currentCategoryProfile = target;
            JsonTextBox.Text = CategoryProfileService.LoadText(target);
            Settings.Instance.SelectedCategoryProfile = target;
            Settings.Instance.Save();
        }

        private async void NewCategoryProfile_Click(object sender, RoutedEventArgs e)
        {
            var input = await DialogHelper.ShowInputAsync(XamlRoot, "New Category Profile", "Profile name");
            if (input == null) return;

            var name = CategoryProfileService.SanitizeName(input);
            if (name.Length == 0) { ShowStatus("Please enter a valid name.", Microsoft.UI.Colors.Red); return; }
            if (CategoryProfileService.Exists(name)) { ShowStatus($"A profile named '{name}' already exists.", Microsoft.UI.Colors.Red); return; }

            // Preserve any valid edits to the current profile before switching away.
            if (_currentCategoryProfile != null && CategoryProfileService.TryValidate(JsonTextBox.Text, out _))
                CategoryProfileService.WriteText(_currentCategoryProfile, JsonTextBox.Text);

            try { CategoryProfileService.Create(name); }
            catch (Exception ex) { ShowStatus(ex.Message, Microsoft.UI.Colors.Red); return; }

            Settings.Instance.SelectedCategoryProfile = name;
            Settings.Instance.Save();
            PopulateCategoryProfiles();
            ShowStatus($"Created profile '{name}'.", Microsoft.UI.Colors.LimeGreen);
        }

        private async void RenameCategoryProfile_Click(object sender, RoutedEventArgs e)
        {
            if (_currentCategoryProfile == null) return;

            var input = await DialogHelper.ShowInputAsync(XamlRoot, "Rename Category Profile", "New name", _currentCategoryProfile);
            if (input == null) return;

            var name = CategoryProfileService.SanitizeName(input);
            if (name.Length == 0) { ShowStatus("Please enter a valid name.", Microsoft.UI.Colors.Red); return; }
            if (string.Equals(name, _currentCategoryProfile, StringComparison.OrdinalIgnoreCase)) return;
            if (CategoryProfileService.Exists(name)) { ShowStatus($"A profile named '{name}' already exists.", Microsoft.UI.Colors.Red); return; }

            // Persist the open editor into the existing file before moving it.
            if (!CategoryProfileService.TryValidate(JsonTextBox.Text, out var err))
            {
                ShowStatus($"Fix the JSON before renaming: {err}", Microsoft.UI.Colors.Red);
                return;
            }
            CategoryProfileService.WriteText(_currentCategoryProfile, JsonTextBox.Text);

            try { CategoryProfileService.Rename(_currentCategoryProfile, name); }
            catch (Exception ex) { ShowStatus(ex.Message, Microsoft.UI.Colors.Red); return; }

            Settings.Instance.SelectedCategoryProfile = name;
            Settings.Instance.Save();
            PopulateCategoryProfiles();
            ShowStatus($"Renamed to '{name}'.", Microsoft.UI.Colors.LimeGreen);
        }

        private async void DeleteCategoryProfile_Click(object sender, RoutedEventArgs e)
        {
            if (_currentCategoryProfile == null) return;
            if (CategoryProfileService.ListProfiles().Count <= 1)
            {
                ShowStatus("At least one category profile is required.", Microsoft.UI.Colors.Red);
                return;
            }

            var confirmed = await DialogHelper.ShowConfirmationAsync(XamlRoot, "Delete Profile",
                $"Delete category profile '{_currentCategoryProfile}'? This cannot be undone.",
                "Delete", "Cancel");
            if (!confirmed) return;

            var deleted = _currentCategoryProfile;
            CategoryProfileService.Delete(deleted);
            Settings.Instance.SelectedCategoryProfile =
                CategoryProfileService.ListProfiles().FirstOrDefault() ?? "Default";
            Settings.Instance.Save();
            PopulateCategoryProfiles();
            ShowStatus($"Deleted profile '{deleted}'.", Microsoft.UI.Colors.LimeGreen);
        }

        private void RestorePromptDefaultsButton_Click(object sender, RoutedEventArgs e)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            JsonTextBox.Text = JsonSerializer.Serialize(new DefaultRandomPrompts(), options);

            ShowStatus("Default categories loaded into the editor. Click 'Save Settings' to apply.", Microsoft.UI.Colors.LimeGreen);
        }

        // ---- Word lists -------------------------------------------------------

        private void PopulateWordLists()
        {
            _suppressProfileEvents = true;
            var lists = RandomWordService.ListWordLists().ToList();
            WordListComboBox.ItemsSource = lists;

            var active = lists.FirstOrDefault(l =>
                            string.Equals(l, Settings.Instance.SelectedWordList, StringComparison.OrdinalIgnoreCase))
                        ?? lists.FirstOrDefault();
            WordListComboBox.SelectedItem = active;
            _suppressProfileEvents = false;

            _currentWordList = active;
            WordListTextBox.Text = active != null ? RandomWordService.LoadText(active) : string.Empty;
        }

        private void WordListComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressProfileEvents) return;
            if (WordListComboBox.SelectedItem is not string target) return;
            if (string.Equals(target, _currentWordList, StringComparison.Ordinal)) return;

            // Auto-save the editor to the previously selected list.
            if (_currentWordList != null)
                RandomWordService.WriteText(_currentWordList, WordListTextBox.Text);

            _currentWordList = target;
            WordListTextBox.Text = RandomWordService.LoadText(target);
            Settings.Instance.SelectedWordList = target;
            Settings.Instance.Save();
        }

        private async void NewWordList_Click(object sender, RoutedEventArgs e)
        {
            var input = await DialogHelper.ShowInputAsync(XamlRoot, "New Word List", "Word list name");
            if (input == null) return;

            var name = RandomWordService.SanitizeName(input);
            if (name.Length == 0) { ShowStatus("Please enter a valid name.", Microsoft.UI.Colors.Red); return; }
            if (RandomWordService.Exists(name)) { ShowStatus($"A word list named '{name}' already exists.", Microsoft.UI.Colors.Red); return; }

            // Preserve edits to the current list before switching away.
            if (_currentWordList != null)
                RandomWordService.WriteText(_currentWordList, WordListTextBox.Text);

            try { RandomWordService.Create(name); }
            catch (Exception ex) { ShowStatus(ex.Message, Microsoft.UI.Colors.Red); return; }

            Settings.Instance.SelectedWordList = name;
            Settings.Instance.Save();
            PopulateWordLists();
            ShowStatus($"Created word list '{name}'.", Microsoft.UI.Colors.LimeGreen);
        }

        private async void RenameWordList_Click(object sender, RoutedEventArgs e)
        {
            if (_currentWordList == null) return;

            var input = await DialogHelper.ShowInputAsync(XamlRoot, "Rename Word List", "New name", _currentWordList);
            if (input == null) return;

            var name = RandomWordService.SanitizeName(input);
            if (name.Length == 0) { ShowStatus("Please enter a valid name.", Microsoft.UI.Colors.Red); return; }
            if (string.Equals(name, _currentWordList, StringComparison.OrdinalIgnoreCase)) return;
            if (RandomWordService.Exists(name)) { ShowStatus($"A word list named '{name}' already exists.", Microsoft.UI.Colors.Red); return; }

            RandomWordService.WriteText(_currentWordList, WordListTextBox.Text);

            try { RandomWordService.Rename(_currentWordList, name); }
            catch (Exception ex) { ShowStatus(ex.Message, Microsoft.UI.Colors.Red); return; }

            Settings.Instance.SelectedWordList = name;
            Settings.Instance.Save();
            PopulateWordLists();
            ShowStatus($"Renamed to '{name}'.", Microsoft.UI.Colors.LimeGreen);
        }

        private async void DeleteWordList_Click(object sender, RoutedEventArgs e)
        {
            if (_currentWordList == null) return;
            if (RandomWordService.ListWordLists().Count <= 1)
            {
                ShowStatus("At least one word list is required.", Microsoft.UI.Colors.Red);
                return;
            }

            var confirmed = await DialogHelper.ShowConfirmationAsync(XamlRoot, "Delete Word List",
                $"Delete word list '{_currentWordList}'? This cannot be undone.",
                "Delete", "Cancel");
            if (!confirmed) return;

            var deleted = _currentWordList;
            RandomWordService.Delete(deleted);
            Settings.Instance.SelectedWordList =
                RandomWordService.ListWordLists().FirstOrDefault() ?? "Default";
            Settings.Instance.Save();
            PopulateWordLists();
            ShowStatus($"Deleted word list '{deleted}'.", Microsoft.UI.Colors.LimeGreen);
        }

        private void RestoreWordsDefaultsButton_Click(object sender, RoutedEventArgs e)
        {
            WordListTextBox.Text = RandomWordService.GetDefaultWordListText();

            ShowStatus("Default word list loaded into the editor. Click 'Save Settings' to apply.", Microsoft.UI.Colors.LimeGreen);
        }

        private void OpenDataFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WallTrek");

                // Create the directory if it doesn't exist
                if (!Directory.Exists(appDataFolder))
                {
                    Directory.CreateDirectory(appDataFolder);
                }

                // Open the folder in Windows Explorer
                Process.Start(new ProcessStartInfo
                {
                    FileName = appDataFolder,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                ShowStatus($"Error opening folder: {ex.Message}", Microsoft.UI.Colors.Red);
            }
        }

        private void ShowStatus(string message, Windows.UI.Color color)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(color);
        }
    }
}
