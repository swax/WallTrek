using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using WallTrek.Services;
using Windows.System;

namespace WallTrek.Views
{
    public sealed partial class PromptHistoryView : UserControl
    {
        public event EventHandler? NavigateBack;
        public event EventHandler<string>? CopyPrompt;
        private readonly DatabaseService databaseService;

        public PromptHistoryView()
        {
            this.InitializeComponent();
            databaseService = new DatabaseService();
        }

        public void RefreshHistory()
        {
            LoadPromptHistory();
        }

        private async void LoadPromptHistory()
        {
            try
            {
                var history = await databaseService.GetPromptHistoryAsync();
                PromptListView.ItemsSource = history;
            }
            catch (Exception ex)
            {
                // Handle error - could show a message to user
                System.Diagnostics.Debug.WriteLine($"Error loading prompt history: {ex.Message}");
            }
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
    }
}