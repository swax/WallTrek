using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace WallTrek.Services.DeviantArt
{
    public class DeviantArtUploadService
    {
        private readonly DatabaseService databaseService;
        private readonly TitleService titleService;
        private readonly DeviantArtAuthService authService;

        public DeviantArtUploadService()
        {
            databaseService = new DatabaseService();
            titleService = new TitleService();
            authService = new DeviantArtAuthService();
        }

        public async Task UploadImageAsync(ImageHistoryItem imageItem, string? promptText, XamlRoot xamlRoot, Action refreshCallback)
        {
            if (imageItem.IsUploadedToDeviantArt)
            {
                return;
            }

            try
            {
                var deviantArtService = new DeviantArtService();
                
                if (string.IsNullOrEmpty(promptText))
                {
                    throw new InvalidOperationException("Cannot generate title and tags: prompt text is null or empty");
                }
                
                var titleResult = await titleService.GenerateTitleAndTagsAsync(promptText);
                ValidateTitleResult(titleResult);
                
                var title = titleResult!.Title;
                var tags = titleResult.Tags;
                var description = CreateDescription(promptText);

                var result = await deviantArtService.UploadImageAsync(imageItem.ImagePath, title, description, tags);
                
                if (result.Success)
                {
                    await HandleSuccessfulUpload(imageItem, result.DeviantArtUrl, refreshCallback);
                }
                else
                {
                    await HandleUploadFailure(result, imageItem, promptText, xamlRoot, refreshCallback);
                }
            }
            catch (Exception ex)
            {
                await ShowUploadErrorDialog(ex, xamlRoot);
            }
        }

        private void ValidateTitleResult(TitleResult? titleResult)
        {
            if (titleResult == null)
            {
                throw new InvalidOperationException("Title generation failed: returned null result");
            }

            if (string.IsNullOrEmpty(titleResult.Title))
            {
                throw new InvalidOperationException("Title generation failed: returned null or empty title");
            }
            
            if (titleResult.Tags == null || titleResult.Tags.Length == 0)
            {
                throw new InvalidOperationException("Tag generation failed: returned null tags");
            }
        }

        private string CreateDescription(string promptText)
        {
            return !string.IsNullOrEmpty(promptText) ? 
                $"Prompt: {promptText}\n\nGenerated with WallTrek - AI-powered wallpaper generator" :
                "Generated with WallTrek - AI-powered wallpaper generator";
        }

        private async Task HandleSuccessfulUpload(ImageHistoryItem imageItem, string? deviantArtUrl, Action refreshCallback)
        {
            await databaseService.SetDeviantArtUploadAsync(imageItem.ImagePath, true, deviantArtUrl);
            
            imageItem.IsUploadedToDeviantArt = true;
            imageItem.DeviantArtUrl = deviantArtUrl;
            
            refreshCallback?.Invoke();
            
            System.Diagnostics.Debug.WriteLine($"Successfully uploaded to DeviantArt: {deviantArtUrl}");
        }

        private async Task HandleUploadFailure(DeviantArtUploadResult result, ImageHistoryItem imageItem, string? promptText, XamlRoot xamlRoot, Action refreshCallback)
        {
            if (IsAuthorizationError(result.ErrorMessage))
            {
                var authSuccess = await authService.AuthorizeAndUploadAsync(imageItem, promptText, xamlRoot);
                if (authSuccess)
                {
                    await UploadImageAsync(imageItem, promptText, xamlRoot, refreshCallback);
                }
            }
            else
            {
                await ShowGenericUploadErrorDialog(result.ErrorMessage, xamlRoot);
            }
        }

        private bool IsAuthorizationError(string? errorMessage)
        {
            return errorMessage?.Contains("authorization") == true || 
                   errorMessage?.Contains("user authorization required") == true;
        }

        private async Task ShowGenericUploadErrorDialog(string? errorMessage, XamlRoot xamlRoot)
        {
            var errorDialog = new ContentDialog
            {
                Title = "Upload Failed",
                Content = errorMessage ?? "Unknown error occurred during upload.",
                CloseButtonText = "OK",
                XamlRoot = xamlRoot
            };
            await errorDialog.ShowAsync();
        }

        private async Task ShowUploadErrorDialog(Exception ex, XamlRoot xamlRoot)
        {
            var errorDialog = new ContentDialog
            {
                Title = "Upload Error",
                Content = $"An error occurred while uploading: {ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = xamlRoot
            };
            await errorDialog.ShowAsync();
        }
    }
}