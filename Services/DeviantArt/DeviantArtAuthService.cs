using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.System;

namespace WallTrek.Services.DeviantArt
{
    public class DeviantArtAuthService
    {
        public async Task<bool> AuthorizeAndUploadAsync(ImageHistoryItem imageItem, string? promptText, XamlRoot xamlRoot)
        {
            var settings = Settings.Instance;
            if (string.IsNullOrEmpty(settings.DeviantArtClientId))
            {
                await ShowConfigurationRequiredDialog(xamlRoot);
                return false;
            }

            var authDialog = new ContentDialog
            {
                Title = "DeviantArt Authorization Required",
                Content = "To upload images to DeviantArt, you need to authorize this application.\n\n" +
                         "Click 'Authorize' to open DeviantArt in your browser, then copy the authorization code that appears in the URL after you approve the application.\n\n" +
                         "Look for: ?code=XXXXXXX in the browser URL after authorization.",
                PrimaryButtonText = "Authorize",
                CloseButtonText = "Cancel",
                XamlRoot = xamlRoot
            };

            var result = await authDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var deviantArtService = new DeviantArtService();
                var authUrl = deviantArtService.GetAuthorizationUrl(settings.DeviantArtClientId);
                
                await Launcher.LaunchUriAsync(new Uri(authUrl));
                
                return await HandleCodeInput(imageItem, promptText, deviantArtService, xamlRoot);
            }

            return false;
        }

        private async Task ShowConfigurationRequiredDialog(XamlRoot xamlRoot)
        {
            var configDialog = new ContentDialog
            {
                Title = "DeviantArt Configuration Required",
                Content = "Please configure your DeviantArt Client ID and Secret in Settings first.",
                CloseButtonText = "OK",
                XamlRoot = xamlRoot
            };
            await configDialog.ShowAsync();
        }

        private async Task<bool> HandleCodeInput(ImageHistoryItem imageItem, string? promptText, DeviantArtService deviantArtService, XamlRoot xamlRoot)
        {
            var codeDialog = new ContentDialog
            {
                Title = "Enter Authorization Code",
                Content = CreateCodeInputContent(),
                PrimaryButtonText = "Complete Authorization",
                CloseButtonText = "Cancel",
                XamlRoot = xamlRoot
            };

            var result = await codeDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var codeTextBox = (TextBox)((StackPanel)codeDialog.Content).Children[1];
                var authCode = codeTextBox.Text.Trim();
                
                if (!string.IsNullOrEmpty(authCode))
                {
                    authCode = ExtractCodeFromInput(authCode);

                    var settings = Settings.Instance;
                    var success = await deviantArtService.ExchangeCodeForTokenAsync(
                        authCode, settings.DeviantArtClientId!, settings.DeviantArtClientSecret!);
                    
                    if (success)
                    {
                        return true;
                    }
                    else
                    {
                        await ShowAuthorizationFailedDialog(xamlRoot);
                    }
                }
            }

            return false;
        }

        private string ExtractCodeFromInput(string input)
        {
            var match = Regex.Match(input, @"code=([^&]+)");
            return match.Success ? match.Groups[1].Value : input;
        }

        private async Task ShowAuthorizationFailedDialog(XamlRoot xamlRoot)
        {
            var errorDialog = new ContentDialog
            {
                Title = "Authorization Failed",
                Content = "Failed to exchange authorization code for access token. Please try again.",
                CloseButtonText = "OK",
                XamlRoot = xamlRoot
            };
            await errorDialog.ShowAsync();
        }

        private StackPanel CreateCodeInputContent()
        {
            var stackPanel = new StackPanel { Spacing = 10 };
            
            var instructionText = new TextBlock
            {
                Text = "After authorizing in your browser, copy the authorization code from the URL and paste it below:\n\n" +
                       "Example URL: http://127.0.0.1:8245/callback?code=XXXXXXX\n" +
                       "Copy the code after 'code=' or paste the entire URL:",
                TextWrapping = TextWrapping.Wrap
            };
            
            var codeTextBox = new TextBox
            {
                PlaceholderText = "Paste authorization code or full URL here...",
                Width = 400
            };
            
            stackPanel.Children.Add(instructionText);
            stackPanel.Children.Add(codeTextBox);
            
            return stackPanel;
        }
    }
}