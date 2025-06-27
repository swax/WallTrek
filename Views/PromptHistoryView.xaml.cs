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
using Windows.System;

namespace WallTrek.Views
{
    public sealed partial class PromptHistoryView : UserControl
    {
        public event EventHandler? NavigateBack;
        public event EventHandler<string>? CopyPrompt;
        private readonly DatabaseService databaseService;
        private List<PromptHistoryItem> allPrompts = new List<PromptHistoryItem>();
        private DispatcherTimer searchDebounceTimer;

        public PromptHistoryView()
        {
            this.InitializeComponent();
            databaseService = new DatabaseService();
            
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
            if (sender is Button button && button.Tag is string imagePath)
            {
                ShowFullScreenImage(imagePath);
            }
        }

        private void ImageButton_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string imagePath)
            {
                var menuFlyout = new MenuFlyout();

                // Set as background option
                var setBackgroundItem = new MenuFlyoutItem
                {
                    Text = "Set as Background",
                    Icon = new FontIcon { Glyph = "\uE91B" }
                };
                setBackgroundItem.Click += (s, args) => SetAsBackground(imagePath);
                menuFlyout.Items.Add(setBackgroundItem);

                // Open image option
                var openImageItem = new MenuFlyoutItem
                {
                    Text = "Open Image",
                    Icon = new FontIcon { Glyph = "\uE8A7" }
                };
                openImageItem.Click += (s, args) => OpenImage(imagePath);
                menuFlyout.Items.Add(openImageItem);

                // Delete image option
                var deleteImageItem = new MenuFlyoutItem
                {
                    Text = "Delete Image",
                    Icon = new FontIcon { Glyph = "\uE74D", Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.OrangeRed) }
                };
                deleteImageItem.Click += (s, args) => DeleteImage(imagePath);
                menuFlyout.Items.Add(deleteImageItem);

                menuFlyout.ShowAt(button);
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PromptHistoryItem item)
            {
                var dialog = new ContentDialog
                {
                    Title = "Delete Prompt",
                    Content = "This will delete the prompt from history. The generated images will still exist in your image folder.\n\nAre you sure you want to continue?",
                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
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
            var dialog = new ContentDialog
            {
                Title = "Delete Image",
                Content = "This will permanently delete the image file and remove it from the history.\n\nAre you sure you want to continue?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
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
    }

    public class FavoriteColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isFavorite && isFavorite)
            {
                return Microsoft.UI.Colors.Gold;
            }
            return Microsoft.UI.Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}