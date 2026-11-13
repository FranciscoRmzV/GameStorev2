using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GameStore
{
    internal static class Dialogs
    {
        public static async Task Info(XamlRoot root, string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true },
                CloseButtonText = "Aceptar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = root
            };
            await dialog.ShowAsync();
        }

        public static async Task<bool> Confirm(XamlRoot root, string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                PrimaryButtonText = "Sí",
                CloseButtonText = "No",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = root
            };
            return await dialog.ShowAsync() == ContentDialogResult.Primary;
        }
    }
}
