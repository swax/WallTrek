using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using WallTrek.Services;

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
    }
}