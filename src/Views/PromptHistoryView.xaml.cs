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
    }
}