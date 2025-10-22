using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WallTrek.Services;
using WallTrek.Services.DeviantArt;
using WallTrek.Utilities;
using Windows.System;

namespace WallTrek.Views
{
    public sealed partial class PromptHistoryView : UserControl
    {
        public event EventHandler? NavigateBack;
        public event EventHandler<string>? CopyPrompt;
        private readonly DatabaseService databaseService;
        private readonly DeviantArtUploadService deviantArtUploadService;
        private List<PromptHistoryItem> allPrompts = new List<PromptHistoryItem>();
        private DispatcherTimer searchDebounceTimer;

        public PromptHistoryView()
        {
            this.InitializeComponent();
            databaseService = new DatabaseService();
            deviantArtUploadService = new DeviantArtUploadService();
            
            // Initialize debounce timer
            searchDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            searchDebounceTimer.Tick += (s, e) =>
            {
                searchDebounceTimer.Stop();
                ApplySearchFilter();
            };
        }

        public void RefreshHistory()
        {
            LoadPromptHistory();
        }

        private async void LoadPromptHistory()
        {
            try
            {
                // Store currently expanded IDs for efficient lookup
                var expandedIds = new HashSet<int>(allPrompts.Where(p => p.IsExpanded).Select(p => p.Id));
                
                var newPrompts = await databaseService.GetPromptHistoryAsync();
                
                // Preserve expanded state from existing prompts
                foreach (var newPrompt in newPrompts)
                {
                    newPrompt.IsExpanded = expandedIds.Contains(newPrompt.Id);
                }
                
                allPrompts = newPrompts;
                ApplySearchFilter();
            }
            catch (Exception ex)
            {
                // Handle error - could show a message to user
                System.Diagnostics.Debug.WriteLine($"Error loading prompt history: {ex.Message}");
            }
        }

        private void ApplySearchFilter()
        {
            var searchText = SearchTextBox?.Text?.Trim() ?? string.Empty;
            
            if (string.IsNullOrEmpty(searchText))
            {
                PromptListView.ItemsSource = allPrompts;
                return;
            }

            var searchWords = searchText.ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var filteredPrompts = allPrompts.Where(prompt =>
            {
                var promptTextLower = prompt.PromptText.ToLowerInvariant();
                return searchWords.All(word => promptTextLower.Contains(word));
            }).ToList();

            PromptListView.ItemsSource = filteredPrompts;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateBack?.Invoke(this, EventArgs.Empty);
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PromptHistoryItem item)
            {
                CopyPrompt?.Invoke(this, item.PromptText);
            }
        }

        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ImageHistoryItem imageItem)
            {
                ShowFullScreenImage(imageItem.ImagePath);
            }
        }

        private void ImageButton_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            // This method is now handled by the XAML ContextFlyout
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PromptHistoryItem item)
            {
                var confirmed = await DialogHelper.ShowConfirmationAsync(
                    this.XamlRoot,
                    "Delete Prompt",
                    "This will delete the prompt from history. The generated images will still exist in your image folder.\n\nAre you sure you want to continue?",
                    "Delete",
                    "Cancel");

                if (confirmed)
                {
                    try
                    {
                        await databaseService.DeletePromptAsync(item.Id);
                        LoadPromptHistory(); // Refresh the list
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error deleting prompt: {ex.Message}");
                    }
                }
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

        private async void DeleteImage(string imagePath)
        {
            var confirmed = await DialogHelper.ShowConfirmationAsync(
                this.XamlRoot,
                "Delete Image",
                "This will permanently delete the image file and remove it from the history.\n\nAre you sure you want to continue?",
                "Delete",
                "Cancel");

            if (confirmed)
            {
                try
                {
                    // Delete the file
                    if (File.Exists(imagePath))
                    {
                        File.Delete(imagePath);
                    }

                    // Remove from database
                    await databaseService.DeleteImageAsync(imagePath);

                    // Refresh the history
                    LoadPromptHistory();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error deleting image: {ex.Message}");
                }
            }
        }

        private async void UploadToDeviantArt_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && menuItem.Tag is ImageHistoryItem imageItem)
            {
                imageItem.IsUploading = true;
                
                try
                {
                    var promptItem = FindPromptItemForImage(imageItem);
                    await deviantArtUploadService.UploadImageAsync(imageItem, promptItem?.PromptText, this.XamlRoot, LoadPromptHistory);
                }
                finally
                {
                    imageItem.IsUploading = false;
                }
            }
        }

        private async void ViewOnDeviantArt_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && menuItem.Tag is ImageHistoryItem imageItem)
            {
                if (!string.IsNullOrEmpty(imageItem.DeviantArtUrl))
                {
                    try
                    {
                        await Launcher.LaunchUriAsync(new Uri(imageItem.DeviantArtUrl));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error opening DeviantArt URL: {ex.Message}");
                    }
                }
            }
        }

        private void SetAsBackground_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && menuItem.Tag is ImageHistoryItem imageItem)
            {
                SetAsBackground(imageItem.ImagePath);
            }
        }

        private void OpenImage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && menuItem.Tag is ImageHistoryItem imageItem)
            {
                OpenImage(imageItem.ImagePath);
            }
        }

        private async void OpenInExplorer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && menuItem.Tag is ImageHistoryItem imageItem)
            {
                try
                {
                    var folderPath = Path.GetDirectoryName(imageItem.ImagePath);
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


        private PromptHistoryItem? FindPromptItemForImage(ImageHistoryItem imageItem)
        {
            // Find the prompt that contains this image
            return allPrompts.FirstOrDefault(prompt => 
                prompt.ImageItems.Any(img => img.ImagePath == imageItem.ImagePath));
        }


        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Reset and restart the debounce timer
            searchDebounceTimer.Stop();
            searchDebounceTimer.Start();
        }

        private async void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PromptHistoryItem item)
            {
                try
                {
                    // Toggle the UI state first
                    item.IsFavorite = !item.IsFavorite;
                    
                    // Update the database with the explicit new state
                    await databaseService.SetFavoriteAsync(item.Id, item.IsFavorite);
                }
                catch (Exception ex)
                {
                    // Revert the UI state if database update failed
                    item.IsFavorite = !item.IsFavorite;
                    System.Diagnostics.Debug.WriteLine($"Error setting favorite: {ex.Message}");
                }
            }
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

        private void FullScreenOverlay_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            HideFullScreenImage();
        }

        private void FullScreenImage_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            HideFullScreenImage();
        }

        private void ImageContextMenu_Opening(object sender, object e)
        {
            // Context menu opening event - could be used for dynamic menu updates if needed
        }

        private void ImageButton_Loaded(object sender, RoutedEventArgs e)
        {
            // Event handler for when image buttons are loaded - currently unused but available for future enhancements
        }

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