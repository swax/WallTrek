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
                StatusTextBlock.Text = "Please set your OpenAI API key in Settings first.";
                StatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.OrangeRed);
                NavigateToSettings?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (string.IsNullOrWhiteSpace(PromptTextBox.Text))
            {
                StatusTextBlock.Text = "Please enter a prompt for your wallpaper.";
                StatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.OrangeRed);
                return;
            }

            try
            {
                GenerateButton.IsEnabled = false;
                GenerationProgressBar.Visibility = Visibility.Visible;
                StatusTextBlock.Text = "Generating wallpaper...";
                StatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);

                var imageGenerator = new ImageGenerator(Settings.Instance.ApiKey, Settings.Instance.OutputDirectory);
                var filePath = await imageGenerator.GenerateAndSaveImage(PromptTextBox.Text);
                
                Wallpaper.Set(filePath);

                StatusTextBlock.Text = "Wallpaper generated and set successfully!";
                StatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LimeGreen);
                
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
                StatusTextBlock.Text = $"Error: {ex.Message}";
                StatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.OrangeRed);
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
    }
}