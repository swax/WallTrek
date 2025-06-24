// MainView.xaml.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.IO;
using System.Threading.Tasks;
using WallTrek.Services;
using Windows.System;

namespace WallTrek.Views
{
    public sealed partial class MainView : UserControl
    {
        public event EventHandler? NavigateToSettings;
        public event EventHandler? NavigateToHistory;

        public void SetPromptText(string prompt)
        {
            PromptTextBox.Text = prompt;
        }

        public MainView()
        {
            this.InitializeComponent();
            
            // Create output directory
            Directory.CreateDirectory(Settings.Instance.OutputDirectory);
            
            // Load saved prompt
            LoadSettings();
            
            // Connect to auto-generate events
            AutoGenerateService.Instance.NextGenerateTimeUpdated += OnNextGenerateTimeUpdated;
        }
        
        private void OnNextGenerateTimeUpdated(object? sender, string timeText)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                NextGenerateTextBlock.Text = timeText;
            });
        }
        
        public async void TriggerAutoGenerate()
        {
            // Use the last saved prompt for auto-generation
            if (!string.IsNullOrWhiteSpace(Settings.Instance.LastPrompt))
            {
                PromptTextBox.Text = Settings.Instance.LastPrompt;
                await GenerateWallpaper();
            }
        }

        private void LoadSettings()
        {
            var settings = Settings.Instance;
            PromptTextBox.Text = settings.LastPrompt ?? "";
        }

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            await GenerateWallpaper();
        }
        
        private async Task GenerateWallpaper()
        {
            // Save prompt when generating
            Settings.Instance.LastPrompt = PromptTextBox.Text;
            Settings.Instance.Save();

            if (string.IsNullOrEmpty(Settings.Instance.ApiKey))
            {
                SetStatus("Please set your OpenAI API key in Settings first.", Microsoft.UI.Colors.OrangeRed);
                NavigateToSettings?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (string.IsNullOrWhiteSpace(PromptTextBox.Text))
            {
                SetStatus("Please enter a prompt for your wallpaper.", Microsoft.UI.Colors.OrangeRed);
                return;
            }

            try
            {
                GenerateButton.IsEnabled = false;
                GenerationProgressBar.Visibility = Visibility.Visible;
                SetStatus("Generating wallpaper...", Microsoft.UI.Colors.DodgerBlue);

                var imageGenerator = new ImageGenerator(Settings.Instance.ApiKey, Settings.Instance.OutputDirectory);
                var filePath = await imageGenerator.GenerateAndSaveImage(PromptTextBox.Text);
                
                Wallpaper.Set(filePath);

                SetStatus("Wallpaper generated and set successfully!", Microsoft.UI.Colors.LimeGreen);
                
                // Show balloon tip notification
                ((App)Application.Current).ShowBalloonTip("Wallpaper Generated", "New wallpaper has been generated and set as your background!");
                
                // Setup auto-generate timer if enabled and minutes > 0
                if (Settings.Instance.AutoGenerateEnabled && Settings.Instance.AutoGenerateMinutes > 0)
                {
                    AutoGenerateService.Instance.Start(Settings.Instance.AutoGenerateMinutes);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}", Microsoft.UI.Colors.OrangeRed);
            }
            finally
            {
                GenerationProgressBar.Visibility = Visibility.Collapsed;
                GenerateButton.IsEnabled = true;
            }
        }

        private async void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchFolderPathAsync(Settings.Instance.OutputDirectory);
        }

        private void PromptTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter && !e.KeyStatus.IsMenuKeyDown && GenerateButton.IsEnabled)
            {
                e.Handled = true;
                GenerateButton_Click(sender, e);
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToSettings?.Invoke(this, EventArgs.Empty);
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToHistory?.Invoke(this, EventArgs.Empty);
        }

        private void SetStatus(string message, Windows.UI.Color color)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(color);
        }

        private async void RandomPromptButton_Click(object sender, RoutedEventArgs e)
        {
            await GenerateRandomPrompt();
        }

        private async Task GenerateRandomPrompt()
        {
            if (string.IsNullOrEmpty(Settings.Instance.ApiKey))
            {
                SetStatus("Please set your OpenAI API key in Settings first.", Microsoft.UI.Colors.OrangeRed);
                NavigateToSettings?.Invoke(this, EventArgs.Empty);
                return;
            }

            try
            {
                RandomPromptButton.IsEnabled = false;
                SetStatus("Generating random prompt...", Microsoft.UI.Colors.DodgerBlue);

                var promptGenerator = new PromptGeneratorService(Settings.Instance.ApiKey);
                var randomPrompt = await promptGenerator.GenerateRandomPromptAsync();

                PromptTextBox.Text = randomPrompt;
                SetStatus("Random prompt generated!", Microsoft.UI.Colors.LimeGreen);
            }
            catch (Exception ex)
            {
                SetStatus($"Error generating random prompt: {ex.Message}", Microsoft.UI.Colors.OrangeRed);
            }
            finally
            {
                RandomPromptButton.IsEnabled = true;
            }
        }
    }
}