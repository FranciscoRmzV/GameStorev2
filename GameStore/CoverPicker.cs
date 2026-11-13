using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace GameStore
{
    internal static class CoverPicker
    {
        public static async Task<string> PickAsync()
        {
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            foreach (string ext in DataStore.CoverExtensions)
                picker.FileTypeFilter.Add(ext);

            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));

            StorageFile file = await picker.PickSingleFileAsync();
            return file?.Path;
        }
    }
}
