using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.ApplicationModel.DataTransfer;

namespace WallTrek.Views
{
    public static class ImageDetailsDialog
    {
        public static async void Show(ImageGridItemViewModel viewModel, XamlRoot xamlRoot)
        {
            var dialog = new ContentDialog
            {
                Title = "Image Details",
                CloseButtonText = "Close",
                XamlRoot = xamlRoot
            };

            var detailsPanel = new StackPanel { Spacing = 12, Margin = new Thickness(0, 10, 0, 0) };

            // Title
            if (!string.IsNullOrEmpty(viewModel.Title))
            {
                var titlePanel = new StackPanel { Spacing = 4 };
                titlePanel.Children.Add(new TextBlock
                {
                    Text = "Title:",
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                });
                titlePanel.Children.Add(new TextBlock
                {
                    Text = viewModel.Title,
                    TextWrapping = TextWrapping.Wrap,
                    IsTextSelectionEnabled = true
                });
                detailsPanel.Children.Add(titlePanel);
            }

            // Tags
            if (!string.IsNullOrEmpty(viewModel.Tags))
            {
                var tagsPanel = new StackPanel { Spacing = 4 };
                tagsPanel.Children.Add(new TextBlock
                {
                    Text = "Tags:",
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                });
                tagsPanel.Children.Add(new TextBlock
                {
                    Text = viewModel.Tags,
                    TextWrapping = TextWrapping.Wrap,
                    IsTextSelectionEnabled = true
                });
                detailsPanel.Children.Add(tagsPanel);
            }

            // Prompt with copy button
            var promptPanel = new StackPanel { Spacing = 4 };
            var promptHeader = new TextBlock
            {
                Text = "Prompt:",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };
            promptPanel.Children.Add(promptHeader);

            var promptRow = new Grid();
            promptRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            promptRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var promptText = new TextBlock
            {
                Text = viewModel.PromptText,
                TextWrapping = TextWrapping.Wrap,
                IsTextSelectionEnabled = true
            };
            Grid.SetColumn(promptText, 0);
            promptRow.Children.Add(promptText);

            var copyButton = new Button
            {
                Content = new FontIcon { Glyph = "\uE8C8", FontSize = 14 },
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(8, 0, 0, 0)
            };
            copyButton.Click += (s, args) =>
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(viewModel.PromptText);
                Clipboard.SetContent(dataPackage);
            };
            Grid.SetColumn(copyButton, 1);
            promptRow.Children.Add(copyButton);

            promptPanel.Children.Add(promptRow);
            detailsPanel.Children.Add(promptPanel);

            // Generated Date
            detailsPanel.Children.Add(new TextBlock
            {
                Text = $"Generated: {viewModel.GeneratedDate:yyyy-MM-dd HH:mm:ss}",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });

            // LLM Model
            detailsPanel.Children.Add(new TextBlock
            {
                Text = $"LLM Model: {(string.IsNullOrEmpty(viewModel.LlmModel) ? "N/A" : viewModel.LlmModel)}"
            });

            // Image Model
            detailsPanel.Children.Add(new TextBlock
            {
                Text = $"Image Model: {(string.IsNullOrEmpty(viewModel.ImgModel) ? "N/A" : viewModel.ImgModel)}"
            });

            // Favorite Status
            detailsPanel.Children.Add(new TextBlock
            {
                Text = $"Favorite: {(viewModel.IsFavorite ? "Yes" : "No")}"
            });

            // Upload Status
            detailsPanel.Children.Add(new TextBlock
            {
                Text = $"Uploaded to DeviantArt: {(viewModel.IsUploaded ? "Yes" : "No")}"
            });

            // DeviantArt URL (if uploaded)
            if (viewModel.IsUploaded && !string.IsNullOrEmpty(viewModel.DeviantArtUrl))
            {
                var urlPanel = new StackPanel { Spacing = 4 };
                urlPanel.Children.Add(new TextBlock
                {
                    Text = "DeviantArt URL:",
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                });

                var hyperlinkButton = new HyperlinkButton
                {
                    Content = viewModel.DeviantArtUrl,
                    NavigateUri = new Uri(viewModel.DeviantArtUrl)
                };
                urlPanel.Children.Add(hyperlinkButton);
                detailsPanel.Children.Add(urlPanel);
            }

            // File Path
            var filePathPanel = new StackPanel { Spacing = 4 };
            filePathPanel.Children.Add(new TextBlock
            {
                Text = "File Path:",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            filePathPanel.Children.Add(new TextBlock
            {
                Text = viewModel.ImagePath,
                TextWrapping = TextWrapping.Wrap,
                IsTextSelectionEnabled = true
            });
            detailsPanel.Children.Add(filePathPanel);

            dialog.Content = new ScrollViewer
            {
                Content = detailsPanel,
                MaxHeight = 500
            };

            await dialog.ShowAsync();
        }
    }
}
