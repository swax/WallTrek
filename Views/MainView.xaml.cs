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
            
            // Populate the LLM dropdown from the catalog and select the saved model.
            LlmSelectionComboBox.ItemsSource = LlmModelCatalog.Options;
            LlmSelectionComboBox.SelectedItem =
                LlmModelCatalog.FindById(settings.SelectedLlmModel) ?? LlmModelCatalog.Default;
            
            // Populate the Image Model dropdown from the catalog and select the saved option.
            // Legacy settings stored a bare model id, so fall back to matching by model id.
            ImageModelSelectionComboBox.ItemsSource = ImageModelCatalog.Options;
            ImageModelSelectionComboBox.SelectedItem =
                ImageModelCatalog.FindById(settings.SelectedImageModel)
                ?? ImageModelCatalog.FindByModelId(settings.SelectedImageModel)
                ?? ImageModelCatalog.Default;

            // Populate the Upscale dropdown and select the saved option.
            UpscalerSelectionComboBox.ItemsSource = UpscalerCatalog.Options;
            UpscalerSelectionComboBox.SelectedItem =
                UpscalerCatalog.FindById(settings.SelectedUpscaler) ?? UpscalerCatalog.Default;

            UpdateCostEstimate();
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

            // Resolve the selected image option (from the dropdown, falling back to saved settings).
            var selectedOption = ImageModelSelectionComboBox.SelectedItem as ImageModelOption
                ?? ImageModelCatalog.FindById(Settings.Instance.SelectedImageModel)
                ?? ImageModelCatalog.Default;

            // Check the required API key for the selected provider.
            if (selectedOption.Provider == ImageProvider.Google && string.IsNullOrEmpty(Settings.Instance.GoogleApiKey))
            {
                SetStatus("Please set your Google API key in Settings first.", Microsoft.UI.Colors.OrangeRed);
                NavigateToSettings?.Invoke(this, EventArgs.Empty);
                return;
            }
            else if (selectedOption.Provider == ImageProvider.OpenAI && string.IsNullOrEmpty(Settings.Instance.ApiKey))
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
                Logger.Info($"Generating wallpaper — image: {selectedOption.Id}, LLM: {Settings.Instance.SelectedLlmModel}");

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

                var imageGenerator = ImageGenerationServiceFactory.CreateService(selectedOption);

                // Get current model selections from UI
                var currentLlmModel = (LlmSelectionComboBox.SelectedItem as LlmModelOption)?.ModelId;
                var currentImgModel = selectedOption.ModelId;

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

                // Upscale image (Stability AI) when a tier other than "None" is selected.
                var selectedUpscaler = UpscalerSelectionComboBox.SelectedItem as UpscalerOption
                    ?? UpscalerCatalog.FindById(Settings.Instance.SelectedUpscaler)
                    ?? UpscalerCatalog.Default;

                if (selectedUpscaler.Mode != UpscaleMode.None && string.IsNullOrEmpty(Settings.Instance.StabilityApiKey))
                {
                    Logger.Warn($"Upscaler '{selectedUpscaler.Id}' selected but no Stability API key is set — skipping upscale.");
                }
                else if (selectedUpscaler.Mode != UpscaleMode.None)
                {
                    try
                    {
                        SetStatus($"Upscaling image ({selectedUpscaler.Name})...", Microsoft.UI.Colors.DodgerBlue);
                        var upscaleService = new UpscaleService(Settings.Instance.StabilityApiKey);
                        result.ImageData = await upscaleService.UpscaleImageAsync(result.ImageData, selectedUpscaler.Mode, PromptTextBox.Text, _cancellationTokenSource.Token);
                        tags.Add("stability_ai");
                        tags.Add("4k");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Image upscaling failed; proceeding with original image", ex);
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
                Logger.Error("Wallpaper generation failed", ex);
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
            RandomImageButton.IsEnabled = !generating;
            CancelButton.Visibility = generating ? Visibility.Visible : Visibility.Collapsed;
            RandomPromptButton.IsEnabled = !generating;
            PromptTextBox.IsEnabled = !generating;
            GenerationProgressBar.Visibility = generating ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void RandomPromptButton_Click(object sender, RoutedEventArgs e)
        {
            await GenerateRandomPrompt();
        }

        private async Task<bool> GenerateRandomPrompt()
        {
            if (string.IsNullOrEmpty(Settings.Instance.ApiKey))
            {
                SetStatus("Please set your OpenAI API key in Settings first.", Microsoft.UI.Colors.OrangeRed);
                NavigateToSettings?.Invoke(this, EventArgs.Empty);
                return false;
            }

            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                SetGeneratingState(true);
                SetStatus("Generating random prompt...", Microsoft.UI.Colors.DodgerBlue);
                Logger.Info($"Generating random prompt — LLM: {Settings.Instance.SelectedLlmModel}");

                var promptGenerator = new PromptGeneratorService();
                var result = await promptGenerator.GenerateRandomPromptAsync(_cancellationTokenSource.Token);

                if (result != null)
                {
                    _currentPromptGenerationResult = result;
                    PromptTextBox.Text = result.Prompt;
                    PropertiesBadgesControl.ItemsSource = result.SelectedProperties;
                    SetStatus("Random prompt generated!", Microsoft.UI.Colors.LimeGreen);
                    return true;
                }

                SetStatus("Failed to generate random prompt.", Microsoft.UI.Colors.OrangeRed);
                return false;
            }
            catch (OperationCanceledException)
            {
                SetStatus("Random prompt generation cancelled.", Microsoft.UI.Colors.Orange);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error("Random prompt generation failed", ex);
                SetStatus($"Error generating random prompt: {ex.Message}", Microsoft.UI.Colors.OrangeRed);
                return false;
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
            if (LlmSelectionComboBox.SelectedItem is LlmModelOption option)
            {
                Settings.Instance.SelectedLlmModel = option.ModelId;
                Settings.Instance.Save();
            }
            UpdateCostEstimate();
        }

        private void ImageModelSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ImageModelSelectionComboBox.SelectedItem is ImageModelOption option)
            {
                Settings.Instance.SelectedImageModel = option.Id;
                Settings.Instance.Save();
            }
            UpdateCostEstimate();
        }

        private void UpscalerSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UpscalerSelectionComboBox.SelectedItem is UpscalerOption option)
            {
                Settings.Instance.SelectedUpscaler = option.Id;
                Settings.Instance.Save();
            }
            UpdateCostEstimate();
        }

        private async void RandomImageButton_Click(object sender, RoutedEventArgs e)
        {
            // Full pipeline: generate a random prompt, then generate the image from it.
            if (await GenerateRandomPrompt())
            {
                await GenerateWallpaper();
            }
        }

        // Updates the "Random Image" button label and the cost line from the current
        // LLM + image model + upscaler selections.
        private void UpdateCostEstimate()
        {
            if (RandomImageButton is null || CostEstimateTextBlock is null)
            {
                return;
            }

            var llm = LlmSelectionComboBox.SelectedItem as LlmModelOption ?? LlmModelCatalog.Default;
            var img = ImageModelSelectionComboBox.SelectedItem as ImageModelOption ?? ImageModelCatalog.Default;
            var up = UpscalerSelectionComboBox.SelectedItem as UpscalerOption ?? UpscalerCatalog.Default;

            decimal total = llm.Cents + img.Cents + up.Cents;

            RandomImageButton.Content = $"Random Image  (~{total:0}¢)";
            CostEstimateTextBlock.Text = $"≈ {llm.Cents:0.#}¢ prompt + {img.Cents:0.#}¢ image + {up.Cents:0.#}¢ upscale";
        }
    }
}