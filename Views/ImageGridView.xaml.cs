using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WallTrek.Services;
using WallTrek.Services.DeviantArt;
using WallTrek.Utilities;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.System;

namespace WallTrek.Views
{
    public sealed partial class ImageGridView : UserControl
    {
        public event EventHandler? NavigateBack;
        private ObservableCollection<ImageGridItemViewModel> _displayedImages = new ObservableCollection<ImageGridItemViewModel>();
        private List<ImageGridItemViewModel> _allImages = new List<ImageGridItemViewModel>();
        private List<ImageGridItemViewModel> _filteredImages = new List<ImageGridItemViewModel>();
        private int _displayCount = 100;
        private int _currentDisplayed = 0;
        private readonly DeviantArtUploadService deviantArtUploadService;

        public ImageGridView()
        {
            this.InitializeComponent();
            deviantArtUploadService = new DeviantArtUploadService();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateBack?.Invoke(this, EventArgs.Empty);
        }

        public async void LoadImages()
        {
            _displayedImages.Clear();
            _allImages.Clear();
            _filteredImages.Clear();
            _currentDisplayed = 0;

            var databaseService = new DatabaseService();
            var images = await databaseService.GetAllImagesAsync();

            foreach (var image in images)
            {
                if (File.Exists(image.ImagePath))
                {
                    var viewModel = new ImageGridItemViewModel
                    {
                        ImagePath = image.ImagePath,
                        PromptText = image.PromptText,
                        GeneratedDate = image.GeneratedDate,
                        IsFavorite = image.IsFavorite,
                        IsUploaded = image.IsUploaded,
                        DeviantArtUrl = image.DeviantArtUrl,
                        LlmModel = image.LlmModel,
                        ImgModel = image.ImgModel,
                        Title = image.Title,
                        Tags = image.Tags
                    };

                    _allImages.Add(viewModel);
                }
            }

            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var favoritesOnly = FavoritesOnlyCheckBox.IsChecked == true;
            var notUploadedOnly = NotUploadedOnlyCheckBox.IsChecked == true;

            _filteredImages = _allImages.Where(img =>
            {
                if (favoritesOnly && !img.IsFavorite)
                    return false;
                if (notUploadedOnly && img.IsUploaded)
                    return false;
                return true;
            }).ToList();

            _currentDisplayed = 0;
            _displayedImages.Clear();
            ImagesGrid.ItemsSource = _displayedImages;

            LoadNextBatch();
        }

        private void LoadNextBatch()
        {
            int remaining = _filteredImages.Count - _currentDisplayed;
            int toLoad = Math.Min(_displayCount, remaining);

            for (int i = 0; i < toLoad; i++)
            {
                var viewModel = _filteredImages[_currentDisplayed + i];

                // Load thumbnail asynchronously only if not already loaded
                if (viewModel.ThumbnailImage == null)
                {
                    _ = LoadThumbnailAsync(viewModel);
                }

                _displayedImages.Add(viewModel);
            }

            _currentDisplayed += toLoad;
            UpdateUI();
        }

        private void UpdateUI()
        {
            ImageCountTextBlock.Text = $"{_currentDisplayed}/{_filteredImages.Count} Images";
            var hasMore = _currentDisplayed < _filteredImages.Count;
            LoadMoreButton.Visibility = hasMore ? Visibility.Visible : Visibility.Collapsed;
            LoadAllButton.Visibility = hasMore ? Visibility.Visible : Visibility.Collapsed;
        }

        private void FilterCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_allImages.Count > 0)
            {
                ApplyFilters();
            }
        }

        private void LoadMoreButton_Click(object sender, RoutedEventArgs e)
        {
            LoadNextBatch();
        }

        private void LoadAllButton_Click(object sender, RoutedEventArgs e)
        {
            // Load all remaining images
            while (_currentDisplayed < _filteredImages.Count)
            {
                LoadNextBatch();
            }
        }

        private async Task LoadThumbnailAsync(ImageGridItemViewModel viewModel)
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(viewModel.ImagePath);
                var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.PicturesView, 200);

                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(thumbnail);

                viewModel.ThumbnailImage = bitmapImage;
            }
            catch
            {
                // If thumbnail fails, ignore for now
            }
        }

        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ImageGridItemViewModel viewModel)
            {
                ShowFullScreenImage(viewModel.ImagePath);
            }
        }

        private void ImageButton_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            // This method is handled by the XAML ContextFlyout
        }

        private void ShowFullScreenImage(string imagePath)
        {
            try
            {
                if (File.Exists(imagePath))
                {
                    var bitmap = new BitmapImage(new Uri(imagePath));
                    FullScreenImage.Source = bitmap;
                    FullScreenOverlay.Visibility = Visibility.Visible;
                    FullScreenOverlay.Focus(FocusState.Programmatic);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing fullscreen image: {ex.Message}");
            }
        }

        private void HideFullScreenImage()
        {
            FullScreenOverlay.Visibility = Visibility.Collapsed;
            FullScreenImage.Source = null;
        }

        private void FullScreenOverlay_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                HideFullScreenImage();
                e.Handled = true;
            }
        }

        private void FullScreenOverlay_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            HideFullScreenImage();
        }

        private void FullScreenImage_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            HideFullScreenImage();
        }

        private void SetAsBackground_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && menuItem.Tag is ImageGridItemViewModel viewModel)
            {
                SetAsBackground(viewModel.ImagePath);
            }
        }

        private void SetAsBackground(string imagePath)
        {
            try
            {
                Wallpaper.Set(imagePath);
                // Could show a success message or notification here
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting wallpaper: {ex.Message}");
            }
        }

        private async void UploadToDeviantArt_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && menuItem.Tag is ImageGridItemViewModel viewModel)
            {
                viewModel.IsUploading = true;

                try
                {
                    // Create a temporary ImageHistoryItem for the upload service
                    var imageItem = new ImageHistoryItem
                    {
                        ImagePath = viewModel.ImagePath,
                        IsUploadedToDeviantArt = viewModel.IsUploaded,
                        DeviantArtUrl = viewModel.DeviantArtUrl,
                        LlmModel = viewModel.LlmModel,
                        ImgModel = viewModel.ImgModel
                    };

                    // Parse tags from comma-separated string to array
                    string[]? tags = null;
                    if (!string.IsNullOrWhiteSpace(viewModel.Tags))
                    {
                        tags = viewModel.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    }

                    string? title = !string.IsNullOrWhiteSpace(viewModel.Title) ? viewModel.Title : null;

                    await deviantArtUploadService.UploadImageAsync(imageItem, viewModel.PromptText, this.XamlRoot, () =>
                    {
                        // Update the view model after successful upload
                        viewModel.IsUploaded = imageItem.IsUploadedToDeviantArt;
                        viewModel.DeviantArtUrl = imageItem.DeviantArtUrl ?? string.Empty;
                        LoadImages(); // Refresh the grid
                    }, title, tags);
                }
                finally
                {
                    viewModel.IsUploading = false;
                }
            }
        }

        private async void ViewOnDeviantArt_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && menuItem.Tag is ImageGridItemViewModel viewModel)
            {
                if (!string.IsNullOrEmpty(viewModel.DeviantArtUrl))
                {
                    try
                    {
                        await Launcher.LaunchUriAsync(new Uri(viewModel.DeviantArtUrl));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error opening DeviantArt URL: {ex.Message}");
                    }
                }
            }
        }

        private void OpenImage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && menuItem.Tag is ImageGridItemViewModel viewModel)
            {
                OpenImage(viewModel.ImagePath);
            }
        }

        private async void OpenImage(string imagePath)
        {
            try
            {
                if (File.Exists(imagePath))
                {
                    await Launcher.LaunchUriAsync(new Uri($"file:///{imagePath.Replace('\\', '/')}"));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening image: {ex.Message}");
            }
        }

        private async void OpenInExplorer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && menuItem.Tag is ImageGridItemViewModel viewModel)
            {
                try
                {
                    var folderPath = Path.GetDirectoryName(viewModel.ImagePath);
                    if (Directory.Exists(folderPath))
                    {
                        await Launcher.LaunchUriAsync(new Uri($"file:///{folderPath.Replace('\\', '/')}"));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error opening explorer: {ex.Message}");
                }
            }
        }

        private void ImageContextMenu_Opening(object sender, object e)
        {
            if (sender is MenuFlyout menuFlyout)
            {
                // Find the toggle favorite menu item and update its text
                foreach (var item in menuFlyout.Items)
                {
                    if (item is MenuFlyoutItem menuItem && menuItem.Name == "ToggleFavoriteItem")
                    {
                        if (menuItem.Tag is ImageGridItemViewModel viewModel)
                        {
                            menuItem.Text = viewModel.IsFavorite ? "Remove from Favorites" : "Add to Favorites";
                        }
                        break;
                    }
                }
            }
        }

        private async void ToggleFavorite_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && menuItem.Tag is ImageGridItemViewModel viewModel)
            {
                try
                {
                    var newFavoriteStatus = !viewModel.IsFavorite;

                    // Update database
                    var databaseService = new DatabaseService();
                    await databaseService.SetImageFavoriteAsync(viewModel.ImagePath, newFavoriteStatus);

                    // Update view model
                    viewModel.IsFavorite = newFavoriteStatus;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error toggling favorite status: {ex.Message}");
                }
            }
        }

        private void Details_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && menuItem.Tag is ImageGridItemViewModel viewModel)
            {
                ImageDetailsDialog.Show(viewModel, this.XamlRoot);
            }
        }
    }

    public class ImageGridItemViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        public string ImagePath { get; set; } = string.Empty;
        public string PromptText { get; set; } = string.Empty;
        public DateTime GeneratedDate { get; set; }
        public string DeviantArtUrl { get; set; } = string.Empty;
        public string LlmModel { get; set; } = string.Empty;
        public string ImgModel { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;

        private bool _isFavorite;
        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                if (_isFavorite != value)
                {
                    _isFavorite = value;
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsFavorite)));
                }
            }
        }

        private bool _isUploaded;
        public bool IsUploaded
        {
            get => _isUploaded;
            set
            {
                if (_isUploaded != value)
                {
                    _isUploaded = value;
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsUploaded)));
                }
            }
        }

        private bool _isUploading;
        public bool IsUploading
        {
            get => _isUploading;
            set
            {
                if (_isUploading != value)
                {
                    _isUploading = value;
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsUploading)));
                }
            }
        }

        private BitmapImage? _thumbnailImage;
        public BitmapImage? ThumbnailImage
        {
            get => _thumbnailImage;
            set
            {
                if (_thumbnailImage != value)
                {
                    _thumbnailImage = value;
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(ThumbnailImage)));
                }
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }

    public class FavoriteColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isFavorite && isFavorite)
            {
                return new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gold);
            }
            return new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanNegationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                bool shouldShow = boolValue;

                // Check if we should invert the logic
                if (parameter?.ToString()?.ToLowerInvariant() == "invert")
                {
                    shouldShow = !boolValue;
                }

                return shouldShow ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility visibility)
            {
                bool isVisible = visibility == Visibility.Visible;

                // Check if we should invert the logic
                if (parameter?.ToString()?.ToLowerInvariant() == "invert")
                {
                    return !isVisible;
                }

                return isVisible;
            }
            return false;
        }
    }
}
