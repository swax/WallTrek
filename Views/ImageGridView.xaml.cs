using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WallTrek.Services;
using Windows.Storage;
using Windows.Storage.FileProperties;

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

        public ImageGridView()
        {
            this.InitializeComponent();
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
                        IsUploaded = image.IsUploaded
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
    }

    public class ImageGridItemViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        public string ImagePath { get; set; } = string.Empty;
        public string PromptText { get; set; } = string.Empty;
        public DateTime GeneratedDate { get; set; }
        public bool IsFavorite { get; set; }
        public bool IsUploaded { get; set; }

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
}
