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
using WallTrek.Services.Profiles;
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
        private readonly Random _random = new();
        // Guards the profile/word-list quick-switch dropdowns while we populate them in code.
        private bool _suppressProfileEvents;

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

            // Populate the LLM dropdown from the catalog and restore the saved (multi) selection.
            LlmSelectionControl.CostSelector = o => ((LlmModelOption)o).Cents;
            LlmSelectionControl.ItemsSource = LlmModelCatalog.Options;
            LlmSelectionControl.SetSelectedItems(ResolveLlmSelection());

            // Populate the Image Model dropdown from the catalog and restore the saved selection.
            ImageModelSelectionControl.CostSelector = o => ((ImageModelOption)o).Cents;
            ImageModelSelectionControl.ItemsSource = ImageModelCatalog.Options;
            ImageModelSelectionControl.SetSelectedItems(ResolveImageSelection());

            // Populate the Upscale dropdown and select the saved option.
            UpscalerSelectionComboBox.ItemsSource = UpscalerCatalog.Options;
            UpscalerSelectionComboBox.SelectedItem =
                UpscalerCatalog.FindById(settings.SelectedUpscaler) ?? UpscalerCatalog.Default;

            // Populate the random-prompt profile + word-list quick-switch dropdowns.
            PopulateProfileSelectors();

            UpdateCostEstimate();
        }

        // Resolves the saved LLM checkbox selection, falling back to the legacy single setting
        // (and finally the catalog default) so existing installs keep their previous model.
        private static List<object> ResolveLlmSelection()
        {
            var settings = Settings.Instance;
            var ids = settings.SelectedLlmModels.Count > 0
                ? settings.SelectedLlmModels
                : new List<string> { settings.SelectedLlmModel };

            var options = ids
                .Select(LlmModelCatalog.FindById)
                .Where(o => o is not null)
                .Cast<object>()
                .ToList();

            if (options.Count == 0)
            {
                options.Add(LlmModelCatalog.FindById(settings.SelectedLlmModel) ?? LlmModelCatalog.Default);
            }

            return options;
        }

        private static List<object> ResolveImageSelection()
        {
            var settings = Settings.Instance;
            var ids = settings.SelectedImageModels.Count > 0
                ? settings.SelectedImageModels
                : new List<string> { settings.SelectedImageModel };

            var options = ids
                .Select(id => ImageModelCatalog.FindById(id) ?? ImageModelCatalog.FindByModelId(id))
                .Where(o => o is not null)
                .Cast<object>()
                .ToList();

            if (options.Count == 0)
            {
                options.Add(ImageModelCatalog.FindById(settings.SelectedImageModel)
                    ?? ImageModelCatalog.FindByModelId(settings.SelectedImageModel)
                    ?? ImageModelCatalog.Default);
            }

            return options;
        }

        // When several models are checked, each generation randomly picks one of them.
        private LlmModelOption PickLlmModel()
        {
            var selected = LlmSelectionControl.SelectedItems.Cast<LlmModelOption>().ToList();
            if (selected.Count == 0)
            {
                return LlmModelCatalog.FindById(Settings.Instance.SelectedLlmModel) ?? LlmModelCatalog.Default;
            }
            return selected[_random.Next(selected.Count)];
        }

        private ImageModelOption PickImageModel()
        {
            var selected = ImageModelSelectionControl.SelectedItems.Cast<ImageModelOption>().ToList();
            if (selected.Count == 0)
            {
                return ImageModelCatalog.FindById(Settings.Instance.SelectedImageModel) ?? ImageModelCatalog.Default;
            }
            return selected[_random.Next(selected.Count)];
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

            // Pick the models for this run. When multiple are checked, one is chosen at random.
            var selectedOption = PickImageModel();
            var selectedLlm = PickLlmModel();

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
                Logger.Info($"Generating wallpaper — image: {selectedOption.Id}, LLM: {selectedLlm.ModelId}");

                // If we don't have a cached prompt generation result (e.g., user typed their own prompt),
                // generate title and tags from the prompt
                if (_currentPromptGenerationResult == null)
                {
                    SetStatus("Generating title and tags...", Microsoft.UI.Colors.DodgerBlue);
                    var titleService = new TitleService();
                    var titleResult = await titleService.GenerateTitleAndTagsAsync(PromptTextBox.Text, _cancellationTokenSource.Token, selectedLlm.ModelId);

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

                // Record the models actually used for this generation.
                var currentLlmModel = selectedLlm.ModelId;
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

                var selectedLlm = PickLlmModel();
                Logger.Info($"Generating random prompt — LLM: {selectedLlm.ModelId}");

                var promptGenerator = new PromptGeneratorService();
                var result = await promptGenerator.GenerateRandomPromptAsync(_cancellationTokenSource.Token, selectedLlm.ModelId);

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

        private void LlmSelectionControl_SelectionChanged(object? sender, EventArgs e)
        {
            var ids = LlmSelectionControl.SelectedItems.Cast<LlmModelOption>().Select(o => o.ModelId).ToList();
            Settings.Instance.SelectedLlmModels = ids;
            // Keep the legacy single setting valid (used by other paths, e.g. DeviantArt titles).
            if (ids.Count > 0 && !ids.Contains(Settings.Instance.SelectedLlmModel))
            {
                Settings.Instance.SelectedLlmModel = ids[0];
            }
            Settings.Instance.Save();
            UpdateCostEstimate();
        }

        private void ImageModelSelectionControl_SelectionChanged(object? sender, EventArgs e)
        {
            var ids = ImageModelSelectionControl.SelectedItems.Cast<ImageModelOption>().Select(o => o.Id).ToList();
            Settings.Instance.SelectedImageModels = ids;
            if (ids.Count > 0 && !ids.Contains(Settings.Instance.SelectedImageModel))
            {
                Settings.Instance.SelectedImageModel = ids[0];
            }
            Settings.Instance.Save();
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

        // Populates the category-profile and word-list quick-switch dropdowns from the files
        // on disk and selects the active one saved in settings.
        private void PopulateProfileSelectors()
        {
            _suppressProfileEvents = true;

            var profiles = CategoryProfileService.ListProfiles().ToList();
            CategoryProfileSelector.ItemsSource = profiles;
            CategoryProfileSelector.SelectedItem =
                profiles.FirstOrDefault(p => string.Equals(p, Settings.Instance.SelectedCategoryProfile, StringComparison.OrdinalIgnoreCase))
                ?? profiles.FirstOrDefault();

            var lists = RandomWordService.ListWordLists().ToList();
            WordListSelector.ItemsSource = lists;
            WordListSelector.SelectedItem =
                lists.FirstOrDefault(l => string.Equals(l, Settings.Instance.SelectedWordList, StringComparison.OrdinalIgnoreCase))
                ?? lists.FirstOrDefault();

            _suppressProfileEvents = false;
        }

        // Re-reads the lists/selection (e.g., after returning from Settings, where profiles
        // and word lists may have been added, renamed, deleted, or re-selected).
        public void RefreshProfileSelectors() => PopulateProfileSelectors();

        private void CategoryProfileSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressProfileEvents) return;
            if (CategoryProfileSelector.SelectedItem is string name)
            {
                Settings.Instance.SelectedCategoryProfile = name;
                Settings.Instance.Save();
            }
        }

        private void WordListSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressProfileEvents) return;
            if (WordListSelector.SelectedItem is string name)
            {
                Settings.Instance.SelectedWordList = name;
                Settings.Instance.Save();
            }
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

            // When several models are checked, show the average expected cost across them.
            var llmSelected = LlmSelectionControl.SelectedItems.Cast<LlmModelOption>().ToList();
            var imgSelected = ImageModelSelectionControl.SelectedItems.Cast<ImageModelOption>().ToList();
            var up = UpscalerSelectionComboBox.SelectedItem as UpscalerOption ?? UpscalerCatalog.Default;

            decimal llmCents = llmSelected.Count > 0 ? llmSelected.Average(o => o.Cents) : LlmModelCatalog.Default.Cents;
            decimal imgCents = imgSelected.Count > 0 ? imgSelected.Average(o => o.Cents) : ImageModelCatalog.Default.Cents;

            decimal total = llmCents + imgCents + up.Cents;

            RandomImageButton.Content = $"Random Image  (~{total:0}¢)";
            CostEstimateTextBlock.Text = $"≈ {llmCents:0.#}¢ prompt + {imgCents:0.#}¢ image + {up.Cents:0.#}¢ upscale";
        }
    }
}