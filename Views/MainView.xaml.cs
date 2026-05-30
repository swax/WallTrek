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
            await RunGenerationBatch(1, randomPromptEachTime: settings.AutoGenerateSource == "random");
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

            // Random word count (0 = off) is a per-run control on the main page. Suppress the
            // ValueChanged save while restoring the persisted value.
            _suppressProfileEvents = true;
            RandomWordCountNumberBox.Value = settings.RandomWordCount;
            _suppressProfileEvents = false;

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

        private async void GenerateButton_Click(SplitButton sender, SplitButtonClickEventArgs args)
        {
            await RunGenerationBatch(1, randomPromptEachTime: false);
        }

        // Dropdown entries on the Generate Image split button: 2/4/8/16 or a custom count,
        // all from the prompt currently in the text box.
        private async void GenerateBatchMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var count = await ResolveBatchCountAsync(sender);
            if (count > 0)
            {
                await RunGenerationBatch(count, randomPromptEachTime: false);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
        }
        
        // Runs one or more generations as a single cancellable batch. When randomPromptEachTime
        // is true (the Random Image button) a fresh random prompt is generated before every image;
        // otherwise each image uses the prompt currently in the text box.
        private async Task RunGenerationBatch(int count, bool randomPromptEachTime)
        {
            if (count < 1) count = 1;

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            int succeeded = 0;
            try
            {
                SetGeneratingState(true);

                for (int i = 0; i < count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    var batchLabel = count > 1 ? $" ({i + 1} of {count})" : "";

                    if (randomPromptEachTime && !await GenerateRandomPromptCore(token, batchLabel))
                    {
                        // Soft failure (e.g. the model returned nothing) — status already set.
                        break;
                    }

                    await GenerateWallpaperCore(token, batchLabel);
                    succeeded++;
                }

                if (succeeded > 0)
                {
                    if (count > 1)
                    {
                        SetStatus($"Generated {succeeded} of {count} wallpapers successfully!", Microsoft.UI.Colors.LimeGreen);
                        ((App)Application.Current).ShowBalloonTip("Wallpapers Generated", $"{succeeded} new wallpapers were generated; the latest is set as your background!");
                    }
                    else
                    {
                        SetStatus("Wallpaper generated and set successfully!", Microsoft.UI.Colors.LimeGreen);
                        ((App)Application.Current).ShowBalloonTip("Wallpaper Generated", "New wallpaper has been generated and set as your background!");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                SetStatus(succeeded > 0
                    ? $"Cancelled after {succeeded} image{(succeeded == 1 ? "" : "s")}."
                    : "Wallpaper generation cancelled.", Microsoft.UI.Colors.Orange);
            }
            catch (GenerationAbortedException ex)
            {
                SetStatus(ex.Message, Microsoft.UI.Colors.OrangeRed);
                if (ex.NavigateToSettings)
                {
                    NavigateToSettings?.Invoke(this, EventArgs.Empty);
                }
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

        // Generates a single wallpaper from the current prompt and sets it as the desktop background.
        // The busy UI, cancellation token, and success notification are owned by the caller so this can
        // run repeatedly inside a batch. batchLabel is an optional " (n of m)" suffix for status text.
        private async Task GenerateWallpaperCore(CancellationToken token, string batchLabel = "")
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
                throw new GenerationAbortedException("Please set your Google API key in Settings first.", navigateToSettings: true);
            }
            else if (selectedOption.Provider == ImageProvider.OpenAI && string.IsNullOrEmpty(Settings.Instance.ApiKey))
            {
                throw new GenerationAbortedException("Please set your OpenAI API key in Settings first.", navigateToSettings: true);
            }

            if (string.IsNullOrWhiteSpace(PromptTextBox.Text))
            {
                throw new GenerationAbortedException("Please enter a prompt for your wallpaper.", navigateToSettings: false);
            }

            SetStatus($"Generating wallpaper{batchLabel}...", Microsoft.UI.Colors.DodgerBlue);
            Logger.Info($"Generating wallpaper — image: {selectedOption.Id}, LLM: {selectedLlm.ModelId}");

            // If we don't have a cached prompt generation result (e.g., user typed their own prompt),
            // generate title and tags from the prompt
            if (_currentPromptGenerationResult == null)
            {
                SetStatus($"Generating title and tags{batchLabel}...", Microsoft.UI.Colors.DodgerBlue);
                var titleService = new TitleService();
                var titleResult = await titleService.GenerateTitleAndTagsAsync(PromptTextBox.Text, token, selectedLlm.ModelId);

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
                throw new GenerationAbortedException("Failed to generate title and tags.", navigateToSettings: false);
            }

            SetStatus($"Generating wallpaper{batchLabel}...", Microsoft.UI.Colors.DodgerBlue);

            var imageGenerator = ImageGenerationServiceFactory.CreateService(selectedOption);

            // Record the models actually used for this generation.
            var currentLlmModel = selectedLlm.ModelId;
            var currentImgModel = selectedOption.ModelId;

            if (string.IsNullOrEmpty(currentLlmModel))
            {
                throw new GenerationAbortedException("Please select an LLM model.", navigateToSettings: false);
            }

            if (string.IsNullOrEmpty(currentImgModel))
            {
                throw new GenerationAbortedException("Please select an image model.", navigateToSettings: false);
            }

            var tags = _currentPromptGenerationResult.Tags.ToList();

            // Generate image
            var result = await imageGenerator.GenerateImage(PromptTextBox.Text, token);

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
                    SetStatus($"Upscaling image ({selectedUpscaler.Name}){batchLabel}...", Microsoft.UI.Colors.DodgerBlue);
                    var upscaleService = new UpscaleService(Settings.Instance.StabilityApiKey);
                    result.ImageData = await upscaleService.UpscaleImageAsync(result.ImageData, selectedUpscaler.Mode, PromptTextBox.Text, token);
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

            // A cancel may have arrived during the synchronous gaps between API calls
            // (e.g. upscale cropping). Honor it before we persist and swap the wallpaper.
            token.ThrowIfCancellationRequested();

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
        }

        private async void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchFolderPathAsync(Settings.Instance.OutputDirectory);
        }

        private async void PromptTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter && !e.KeyStatus.IsMenuKeyDown && GenerateButton.IsEnabled)
            {
                e.Handled = true;
                await RunGenerationBatch(1, randomPromptEachTime: false);
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
            _cancellationTokenSource = new CancellationTokenSource();
            try
            {
                SetGeneratingState(true);
                if (await GenerateRandomPromptCore(_cancellationTokenSource.Token))
                {
                    SetStatus("Random prompt generated!", Microsoft.UI.Colors.LimeGreen);
                }
            }
            catch (OperationCanceledException)
            {
                SetStatus("Random prompt generation cancelled.", Microsoft.UI.Colors.Orange);
            }
            catch (GenerationAbortedException ex)
            {
                SetStatus(ex.Message, Microsoft.UI.Colors.OrangeRed);
                if (ex.NavigateToSettings)
                {
                    NavigateToSettings?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Random prompt generation failed", ex);
                SetStatus($"Error generating random prompt: {ex.Message}", Microsoft.UI.Colors.OrangeRed);
            }
            finally
            {
                SetGeneratingState(false);
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        // Generates a random prompt into the text box. The caller owns the busy UI / cancellation token
        // so this can run standalone (Generate Prompt button) or inside a batch (Random Image).
        // Returns false on a soft failure (model returned nothing); throws to abort the batch.
        private async Task<bool> GenerateRandomPromptCore(CancellationToken token, string batchLabel = "")
        {
            if (string.IsNullOrEmpty(Settings.Instance.ApiKey))
            {
                throw new GenerationAbortedException("Please set your OpenAI API key in Settings first.", navigateToSettings: true);
            }

            SetStatus($"Generating random prompt{batchLabel}...", Microsoft.UI.Colors.DodgerBlue);

            var selectedLlm = PickLlmModel();
            Logger.Info($"Generating random prompt — LLM: {selectedLlm.ModelId}");

            var promptGenerator = new PromptGeneratorService();
            var result = await promptGenerator.GenerateRandomPromptAsync(token, selectedLlm.ModelId);

            if (result != null)
            {
                _currentPromptGenerationResult = result;
                PromptTextBox.Text = result.Prompt;
                PropertiesBadgesControl.ItemsSource = result.SelectedProperties;
                return true;
            }

            SetStatus("Failed to generate random prompt.", Microsoft.UI.Colors.OrangeRed);
            return false;
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

        private void RandomWordCountNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (_suppressProfileEvents) return;
            // NumberBox yields NaN when the field is cleared; treat that as 0 (off).
            var count = double.IsNaN(sender.Value) ? 0 : (int)sender.Value;
            Settings.Instance.RandomWordCount = count;
            Settings.Instance.Save();
        }

        private async void RandomImageButton_Click(SplitButton sender, SplitButtonClickEventArgs args)
        {
            await RunGenerationBatch(1, randomPromptEachTime: true);
        }

        // Dropdown entries on the Random Image split button: 2/4/8/16 or a custom count, each with
        // its own freshly generated random prompt.
        private async void RandomBatchMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var count = await ResolveBatchCountAsync(sender);
            if (count > 0)
            {
                await RunGenerationBatch(count, randomPromptEachTime: true);
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

        // Maps a batch menu item's Tag ("2"/"4"/"8"/"16"/"custom") to an image count.
        // "custom" prompts the user; returns 0 when the user cancels or enters nothing.
        private async Task<int> ResolveBatchCountAsync(object sender)
        {
            if (sender is FrameworkElement element && element.Tag is string tag)
            {
                if (string.Equals(tag, "custom", StringComparison.OrdinalIgnoreCase))
                {
                    return await PromptForCustomCountAsync();
                }

                if (int.TryParse(tag, out var count))
                {
                    return count;
                }
            }

            return 1;
        }

        // Asks the user how many images to generate via a simple NumberBox dialog.
        private async Task<int> PromptForCustomCountAsync()
        {
            var numberBox = new NumberBox
            {
                Minimum = 1,
                Maximum = 100,
                Value = 4,
                SmallChange = 1,
                LargeChange = 5,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                Header = "Number of images to generate (1–100)"
            };

            var dialog = new ContentDialog
            {
                Title = "Generate multiple images",
                Content = numberBox,
                PrimaryButtonText = "Generate",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            {
                return 0;
            }

            return double.IsNaN(numberBox.Value) ? 0 : (int)numberBox.Value;
        }

        // Thrown by the generation cores to abort with a user-facing message; NavigateToSettings
        // requests a jump to the Settings view (e.g. a missing API key).
        private sealed class GenerationAbortedException : Exception
        {
            public bool NavigateToSettings { get; }

            public GenerationAbortedException(string message, bool navigateToSettings)
                : base(message)
            {
                NavigateToSettings = navigateToSettings;
            }
        }
    }
}