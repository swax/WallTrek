// MainWindow.xaml.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.IO;
using System.Threading.Tasks;
using WallTrek.Services;
using Windows.System;

namespace WallTrek
{
    public sealed partial class MainWindow : Window
    {
        private readonly string outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "WallTrek");
        private SettingsWindow? settingsWindow;

        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "WallTrek";
            
            // Set window icon
            this.AppWindow.SetIcon("Assets/walltrek.ico");
            
            // Create output directory
            Directory.CreateDirectory(outputDirectory);
            
            // Load saved prompt
            LoadSettings();
            
            // Connect to auto-generate events
            AutoGenerateService.Instance.NextGenerateTimeUpdated += OnNextGenerateTimeUpdated;
            
            // Handle window state changes to minimize to tray based on setting
            this.AppWindow.Changed += AppWindow_Changed;
        }
        
        private void OnNextGenerateTimeUpdated(object? sender, string timeText)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                NextGenerateTextBlock.Text = timeText;
            });
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
                StatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
                ShowSettings();
                return;
            }

            if (string.IsNullOrWhiteSpace(PromptTextBox.Text))
            {
                StatusTextBlock.Text = "Please enter a prompt for your wallpaper.";
                StatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
                return;
            }

            try
            {
                GenerateButton.IsEnabled = false;
                GenerationProgressBar.Visibility = Visibility.Visible;
                StatusTextBlock.Text = "Generating wallpaper...";
                StatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Blue);

                var imageGenerator = new ImageGenerator(Settings.Instance.ApiKey, outputDirectory);
                var filePath = await imageGenerator.GenerateAndSaveImage(PromptTextBox.Text);
                
                Wallpaper.Set(filePath);

                StatusTextBlock.Text = "Wallpaper generated and set successfully!";
                StatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
                
                // Setup auto-generate timer if enabled and minutes > 0
                if (Settings.Instance.AutoGenerateEnabled && Settings.Instance.AutoGenerateMinutes > 0)
                {
                    AutoGenerateService.Instance.Start(Settings.Instance.AutoGenerateMinutes);
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error: {ex.Message}";
                StatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            }
            finally
            {
                GenerationProgressBar.Visibility = Visibility.Collapsed;
                GenerateButton.IsEnabled = true;
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowSettings();
        }

        private void ShowSettings()
        {
            if (settingsWindow == null)
            {
                settingsWindow = new SettingsWindow();
                settingsWindow.Closed += (s, e) => settingsWindow = null;
            }
            
            settingsWindow.Activate();
        }

        private async void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchFolderPathAsync(outputDirectory);
        }

        private void PromptTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter && !e.KeyStatus.IsMenuKeyDown && GenerateButton.IsEnabled)
            {
                e.Handled = true;
                GenerateButton_Click(sender, e);
            }
        }
    }
}