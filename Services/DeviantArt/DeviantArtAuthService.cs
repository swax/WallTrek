using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WallTrek.Utilities;
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

            var confirmed = await DialogHelper.ShowConfirmationAsync(
                xamlRoot,
                "DeviantArt Authorization Required",
                "To upload images to DeviantArt, you need to authorize this application.\n\n" +
                "Click 'Authorize' to open DeviantArt in your browser, then copy the authorization code that appears in the URL after you approve the application.\n\n" +
                "Look for: ?code=XXXXXXX in the browser URL after authorization.",
                "Authorize",
                "Cancel");

            if (confirmed)
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
            await DialogHelper.ShowMessageAsync(
                xamlRoot,
                "DeviantArt Configuration Required",
                "Please configure your DeviantArt Client ID and Secret in Settings first.");
        }

        private async Task<bool> HandleCodeInput(ImageHistoryItem imageItem, string? promptText, DeviantArtService deviantArtService, XamlRoot xamlRoot)
        {
            var codeInputContent = CreateCodeInputContent();
            var result = await DialogHelper.ShowCustomContentAsync(
                xamlRoot,
                "Enter Authorization Code",
                codeInputContent,
                "Complete Authorization",
                null,
                "Cancel");

            if (result == ContentDialogResult.Primary)
            {
                var codeTextBox = (TextBox)codeInputContent.Children[1];
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
            await DialogHelper.ShowMessageAsync(
                xamlRoot,
                "Authorization Failed",
                "Failed to exchange authorization code for access token. Please try again.");
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