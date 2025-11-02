// MainView.xaml.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WallTrek.Services;
using WallTrek.Services.ImageGen;
using WallTrek.Services.TextGen;
using WallTrek.Utilities;
using Windows.System;

namespace WallTrek.Views
{
    public sealed partial class MainView : UserControl
    {
        public event EventHandler? NavigateToSettings;
        public event EventHandler? NavigateToImageGrid;
        private CancellationTokenSource? _cancellationTokenSource;
        private PromptGenerationResult? _currentPromptGenerationResult;

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
            var settings = Settings.Instance;
            
            if (settings.AutoGenerateSource == "random")
            {
                // Generate a random prompt first
                await GenerateRandomPrompt();
            }
            
            // Generate wallpaper using whatever is in the prompt text box
            await GenerateWallpaper();
        }

        private void LoadSettings()
        {
            var settings = Settings.Instance;
            PromptTextBox.Text = settings.LastPrompt ?? "";
            
            // Set the LLM selection dropdown
            var selectedModel = settings.SelectedLlmModel;
            foreach (ComboBoxItem item in LlmSelectionComboBox.Items)
            {
                if (item.Tag?.ToString() == selectedModel)
                {
                    LlmSelectionComboBox.SelectedItem = item;
                    break;
                }
            }
            
            // Set default if nothing was selected
            if (LlmSelectionComboBox.SelectedItem == null && LlmSelectionComboBox.Items.Count > 0)
            {
                LlmSelectionComboBox.SelectedIndex = 0;
            }
            
            // Set the Image Model selection dropdown
            var selectedImageModel = settings.SelectedImageModel;
            foreach (ComboBoxItem item in ImageModelSelectionComboBox.Items)
            {
                if (item.Tag?.ToString() == selectedImageModel)
                {
                    ImageModelSelectionComboBox.SelectedItem = item;
                    break;
                }
            }
            
            // Set default if nothing was selected
            if (ImageModelSelectionComboBox.SelectedItem == null && ImageModelSelectionComboBox.Items.Count > 0)
            {
                ImageModelSelectionComboBox.SelectedIndex = 0;
            }
        }

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            await GenerateWallpaper();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
        }
        
        private async Task GenerateWallpaper()
        {
            // Save prompt when generating
            Settings.Instance.LastPrompt = PromptTextBox.Text;
            Settings.Instance.Save();

            var selectedImageModel = Settings.Instance.SelectedImageModel;
            
            // Check if required API key is available based on selected image model
            if (selectedImageModel.StartsWith("imagen") && string.IsNullOrEmpty(Settings.Instance.GoogleApiKey))
            {
                SetStatus("Please set your Google API key in Settings first.", Microsoft.UI.Colors.OrangeRed);
                NavigateToSettings?.Invoke(this, EventArgs.Empty);
                return;
            }
            else if (selectedImageModel == "dalle-3" && string.IsNullOrEmpty(Settings.Instance.ApiKey))
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

            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                SetGeneratingState(true);
                SetStatus("Generating wallpaper...", Microsoft.UI.Colors.DodgerBlue);

                // If we don't have a cached prompt generation result (e.g., user typed their own prompt),
                // generate title and tags from the prompt
                if (_currentPromptGenerationResult == null)
                {
                    SetStatus("Generating title and tags...", Microsoft.UI.Colors.DodgerBlue);
                    var titleService = new TitleService();
                    var titleResult = await titleService.GenerateTitleAndTagsAsync(PromptTextBox.Text, _cancellationTokenSource.Token);

                    if (titleResult != null)
                    {
                        _currentPromptGenerationResult = new PromptGenerationResult
                        {
                            Prompt = PromptTextBox.Text,
                            Title = titleResult.Title,
                            Tags = titleResult.Tags
                        };
                    }
                }

                if (_currentPromptGenerationResult == null)
                {
                    SetStatus("Failed to generate title and tags.", Microsoft.UI.Colors.OrangeRed);
                    return;
                }

                SetStatus("Generating wallpaper...", Microsoft.UI.Colors.DodgerBlue);

                var imageGenerator = ImageGenerationServiceFactory.CreateService(Settings.Instance.SelectedImageModel);

                // Get current model selections from UI
                var currentLlmModel = (LlmSelectionComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                var currentImgModel = (ImageModelSelectionComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();

                if (string.IsNullOrEmpty(currentLlmModel))
                {
                    SetStatus("Please select an LLM model.", Microsoft.UI.Colors.OrangeRed);
                    return;
                }

                if (string.IsNullOrEmpty(currentImgModel))
                {
                    SetStatus("Please select an image model.", Microsoft.UI.Colors.OrangeRed);
                    return;
                }

                var tags = _currentPromptGenerationResult.Tags.ToList();

                // Generate image
                var result = await imageGenerator.GenerateImage(PromptTextBox.Text, _cancellationTokenSource.Token);

                // Upscale image
                if (!string.IsNullOrEmpty(Settings.Instance.StabilityApiKey))
                {
                    try
                    {
                        SetStatus("Upscaling image...", Microsoft.UI.Colors.DodgerBlue);
                        var upscaleService = new UpscaleService(Settings.Instance.StabilityApiKey);
                        result.ImageData = await upscaleService.UpscaleImageAsync(result.ImageData, _cancellationTokenSource.Token);
                        tags.Add("stability_ai");
                        tags.Add("4k");
                    }
                    catch (Exception ex)
                    {
                        await DialogHelper.ShowMessageAsync(
                            this.XamlRoot,
                            "Image Upscaling Failed",
                            $"Image upscaling failed with {ex.Message}. Proceeding with original image.");
                    }
                }

                // Save image with metadata
                var fileService = new FileService(Settings.Instance.OutputDirectory);
                var title = _currentPromptGenerationResult.Title;
                var tagString = string.Join(", ", tags);
                var metadata = $"Title: {title}\nPrompt: {PromptTextBox.Text}\nTags: {tagString}";
                var filePath = fileService.SaveImageWithMetadata(result.ImageData, metadata, title);

                // Register in database
                var databaseService = new DatabaseService();
                await databaseService.AddGeneratedImageAsync(filePath, currentLlmModel, currentImgModel, PromptTextBox.Text, title, tagString);

                Wallpaper.Set(filePath);

                SetStatus("Wallpaper generated and set successfully!", Microsoft.UI.Colors.LimeGreen);

                // Show balloon tip notification
                ((App)Application.Current).ShowBalloonTip("Wallpaper Generated", "New wallpaper has been generated and set as your background!");
            }
            catch (OperationCanceledException)
            {
                SetStatus("Wallpaper generation cancelled.", Microsoft.UI.Colors.Orange);
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}", Microsoft.UI.Colors.OrangeRed);
            }
            finally
            {
                SetGeneratingState(false);
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
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

        private void PromptTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Set _currentPromptGenerationResult to null if the current prompt text does not equal the cached prompt
            if (_currentPromptGenerationResult != null && PromptTextBox.Text != _currentPromptGenerationResult.Prompt)
            {
                _currentPromptGenerationResult = null;
                PropertiesBadgesControl.ItemsSource = null;
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToSettings?.Invoke(this, EventArgs.Empty);
        }

        private void ImageGridButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToImageGrid?.Invoke(this, EventArgs.Empty);
        }

        private void SetStatus(string message, Windows.UI.Color color)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(color);
        }

        private void SetGeneratingState(bool generating)
        {
            GenerateButton.IsEnabled = !generating;
            CancelButton.Visibility = generating ? Visibility.Visible : Visibility.Collapsed;
            RandomPromptButton.IsEnabled = !generating;
            PromptTextBox.IsEnabled = !generating;
            GenerationProgressBar.Visibility = generating ? Visibility.Visible : Visibility.Collapsed;
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

            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                SetGeneratingState(true);
                SetStatus("Generating random prompt...", Microsoft.UI.Colors.DodgerBlue);

                var promptGenerator = new PromptGeneratorService();
                var result = await promptGenerator.GenerateRandomPromptAsync(_cancellationTokenSource.Token);

                if (result != null)
                {
                    _currentPromptGenerationResult = result;
                    PromptTextBox.Text = result.Prompt;
                    PropertiesBadgesControl.ItemsSource = result.SelectedProperties;
                    SetStatus("Random prompt generated!", Microsoft.UI.Colors.LimeGreen);
                }
                else
                {
                    SetStatus("Failed to generate random prompt.", Microsoft.UI.Colors.OrangeRed);
                }
            }
            catch (OperationCanceledException)
            {
                SetStatus("Random prompt generation cancelled.", Microsoft.UI.Colors.Orange);
            }
            catch (Exception ex)
            {
                SetStatus($"Error generating random prompt: {ex.Message}", Microsoft.UI.Colors.OrangeRed);
            }
            finally
            {
                SetGeneratingState(false);
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void LlmSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LlmSelectionComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                var selectedModel = selectedItem.Tag.ToString();
                if (selectedModel != null)
                {
                    Settings.Instance.SelectedLlmModel = selectedModel;
                    Settings.Instance.Save();
                }
            }
        }

        private void ImageModelSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ImageModelSelectionComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                var selectedModel = selectedItem.Tag.ToString();
                if (selectedModel != null)
                {
                    Settings.Instance.SelectedImageModel = selectedModel;
                    Settings.Instance.Save();
                }
            }
        }
    }
}