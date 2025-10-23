using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using WallTrek.Services;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace WallTrek.Views
{
    public sealed partial class ImageDetailsDialog : UserControl, INotifyPropertyChanged
    {
        private ImageGridItemViewModel? _viewModel;
        private string _imageDimensions = "";
        private string _fileSizeText = "";
        private bool _isLoaded = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ImageDetailsDialog()
        {
            this.InitializeComponent();
            this.Loaded += OnLoaded;
        }

        public ImageGridItemViewModel? ViewModel
        {
            get => _viewModel;
            set
            {
                _viewModel = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowTitle));
                OnPropertyChanged(nameof(ShowTags));
                OnPropertyChanged(nameof(GeneratedDateText));
                OnPropertyChanged(nameof(LlmModelText));
                OnPropertyChanged(nameof(ImgModelText));
                OnPropertyChanged(nameof(FavoriteText));
                OnPropertyChanged(nameof(UploadStatusText));
                OnPropertyChanged(nameof(ShowDeviantArtUrl));
                OnPropertyChanged(nameof(DeviantArtUri));

                if (_isLoaded)
                {
                    _ = LoadImageInfoAsync();
                }
            }
        }

        public Visibility ShowTitle => !string.IsNullOrEmpty(_viewModel?.Title) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ShowTags => !string.IsNullOrEmpty(_viewModel?.Tags) ? Visibility.Visible : Visibility.Collapsed;

        public string ImageDimensions
        {
            get => _imageDimensions;
            private set
            {
                _imageDimensions = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowImageDimensions));
            }
        }

        public Visibility ShowImageDimensions => !string.IsNullOrEmpty(_imageDimensions) ? Visibility.Visible : Visibility.Collapsed;

        public string FileSizeText
        {
            get => _fileSizeText;
            private set
            {
                _fileSizeText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowFileSize));
            }
        }

        public Visibility ShowFileSize => !string.IsNullOrEmpty(_fileSizeText) ? Visibility.Visible : Visibility.Collapsed;

        public string GeneratedDateText => $"Generated: {_viewModel?.GeneratedDate:yyyy-MM-dd HH:mm:ss}";
        public string LlmModelText => $"LLM Model: {(string.IsNullOrEmpty(_viewModel?.LlmModel) ? "N/A" : _viewModel.LlmModel)}";
        public string ImgModelText => $"Image Model: {(string.IsNullOrEmpty(_viewModel?.ImgModel) ? "N/A" : _viewModel.ImgModel)}";
        public string FavoriteText => $"Favorite: {(_viewModel?.IsFavorite == true ? "Yes" : "No")}";
        public string UploadStatusText => $"Uploaded to DeviantArt: {(_viewModel?.IsUploaded == true ? "Yes" : "No")}";

        public Visibility ShowDeviantArtUrl =>
            _viewModel?.IsUploaded == true && !string.IsNullOrEmpty(_viewModel?.DeviantArtUrl)
                ? Visibility.Visible
                : Visibility.Collapsed;

        public Uri? DeviantArtUri => !string.IsNullOrEmpty(_viewModel?.DeviantArtUrl) ? new Uri(_viewModel.DeviantArtUrl) : null;

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            await LoadImageInfoAsync();
        }

        private async System.Threading.Tasks.Task LoadImageInfoAsync()
        {
            if (_viewModel == null) return;

            try
            {
                var fileInfo = new FileInfo(_viewModel.ImagePath);
                var fileSizeKB = fileInfo.Length / 1024.0;
                var fileSizeMB = fileSizeKB / 1024.0;
                FileSizeText = fileSizeMB >= 1
                    ? $"File Size: {fileSizeMB:F2} MB"
                    : $"File Size: {fileSizeKB:F2} KB";

                // Get image dimensions
                var file = await StorageFile.GetFileFromPathAsync(_viewModel.ImagePath);
                using (var stream = await file.OpenAsync(FileAccessMode.Read))
                {
                    var decoder = await BitmapDecoder.CreateAsync(stream);
                    var width = decoder.PixelWidth;
                    var height = decoder.PixelHeight;

                    ImageDimensions = $"Dimensions: {width} × {height} pixels";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting image info: {ex.Message}");
            }
        }

        private void CopyPromptButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(_viewModel.PromptText);
                Clipboard.SetContent(dataPackage);
            }
        }

        public static async void Show(ImageGridItemViewModel viewModel, XamlRoot xamlRoot, Action<string>? onImageDeleted = null)
        {
            var detailsControl = new ImageDetailsDialog
            {
                ViewModel = viewModel
            };

            var dialog = new ContentDialog
            {
                Title = "Image Details",
                Content = detailsControl,
                CloseButtonText = "Close",
                PrimaryButtonText = "Delete",
                XamlRoot = xamlRoot
            };

            // Style the delete button to be red
            dialog.PrimaryButtonStyle = new Style(typeof(Button));
            dialog.PrimaryButtonStyle.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Windows.UI.Color.FromArgb(255, 196, 43, 28))));
            dialog.PrimaryButtonStyle.Setters.Add(new Setter(Button.ForegroundProperty, new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255))));

            var result = await dialog.ShowAsync();

            // Handle delete action
            if (result == ContentDialogResult.Primary)
            {
                // Show confirmation dialog
                var confirmDialog = new ContentDialog
                {
                    Title = "Confirm Delete",
                    Content = "Are you sure you want to delete this image? This action cannot be undone.\n\nThe file will be permanently deleted from your computer.",
                    PrimaryButtonText = "Yes, Delete",
                    CloseButtonText = "Cancel",
                    XamlRoot = xamlRoot
                };

                // Style the delete confirmation button to be red
                confirmDialog.PrimaryButtonStyle = new Style(typeof(Button));
                confirmDialog.PrimaryButtonStyle.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Windows.UI.Color.FromArgb(255, 196, 43, 28))));
                confirmDialog.PrimaryButtonStyle.Setters.Add(new Setter(Button.ForegroundProperty, new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255))));

                var confirmResult = await confirmDialog.ShowAsync();
                if (confirmResult == ContentDialogResult.Primary)
                {
                    await DeleteImageAsync(viewModel.ImagePath, onImageDeleted);
                }
            }
        }

        private static async System.Threading.Tasks.Task DeleteImageAsync(string imagePath, Action<string>? onImageDeleted)
        {
            try
            {
                // Delete from database
                var databaseService = new DatabaseService();
                await databaseService.DeleteImageAsync(imagePath);

                // Delete file from file system
                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                }

                // Notify parent that image was deleted
                onImageDeleted?.Invoke(imagePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting image: {ex.Message}");
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
