using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;

namespace WallTrek.Utilities
{
    public static class DialogHelper
    {
        /// <summary>
        /// Shows a simple message dialog with an OK button
        /// </summary>
        public static async Task ShowMessageAsync(XamlRoot xamlRoot, string title, string content)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "OK",
                XamlRoot = xamlRoot
            };

            await dialog.ShowAsync();
        }

        /// <summary>
        /// Shows a confirmation dialog with Primary and Cancel buttons
        /// </summary>
        public static async Task<bool> ShowConfirmationAsync(
            XamlRoot xamlRoot,
            string title,
            string content,
            string primaryButtonText = "OK",
            string closeButtonText = "Cancel",
            ContentDialogButton defaultButton = ContentDialogButton.Close)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryButtonText,
                CloseButtonText = closeButtonText,
                DefaultButton = defaultButton,
                XamlRoot = xamlRoot
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        /// <summary>
        /// Shows a dialog with custom content
        /// </summary>
        public static async Task<ContentDialogResult> ShowCustomContentAsync(
            XamlRoot xamlRoot,
            string title,
            object content,
            string? primaryButtonText = null,
            string? secondaryButtonText = null,
            string? closeButtonText = "Cancel",
            ContentDialogButton defaultButton = ContentDialogButton.Close)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryButtonText,
                SecondaryButtonText = secondaryButtonText,
                CloseButtonText = closeButtonText,
                DefaultButton = defaultButton,
                XamlRoot = xamlRoot
            };

            return await dialog.ShowAsync();
        }

        /// <summary>
        /// Shows a single-line text-input dialog. Returns the entered text, or null if the
        /// user cancelled.
        /// </summary>
        public static async Task<string?> ShowInputAsync(
            XamlRoot xamlRoot,
            string title,
            string placeholder = "",
            string initial = "",
            string primaryButtonText = "OK",
            string closeButtonText = "Cancel")
        {
            var textBox = new TextBox
            {
                PlaceholderText = placeholder,
                Text = initial,
                AcceptsReturn = false
            };

            var dialog = new ContentDialog
            {
                Title = title,
                Content = textBox,
                PrimaryButtonText = primaryButtonText,
                CloseButtonText = closeButtonText,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = xamlRoot
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary ? textBox.Text : null;
        }

        /// <summary>
        /// Creates a ContentDialog with XamlRoot already set
        /// </summary>
        public static ContentDialog CreateDialog(XamlRoot xamlRoot)
        {
            return new ContentDialog
            {
                XamlRoot = xamlRoot
            };
        }
    }
}
