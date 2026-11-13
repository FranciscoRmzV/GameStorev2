using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

namespace GameStore
{
    public sealed partial class AdminPage : Page
    {
        private string _pendingCoverPath;

        public AdminPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is User admin)
                WelcomeText.Text = $"Sesión de administrador: {admin.Username}";
            LoadCatalog();
        }

        private void LoadCatalog(int selectId = -1)
        {
            var games = DataStore.Games.ToList();
            CatalogList.ItemsSource = games;
            if (selectId > 0)
                CatalogList.SelectedItem = games.FirstOrDefault(g => g.Id == selectId);
        }

        private async void AddGame_Click(object sender, RoutedEventArgs e)
        {
            string title = TitleBox.Text.Trim();
            string platform = PlatformBox.Text.Trim();

            if (title == "" || platform == "")
            {
                await Dialogs.Info(XamlRoot, "Datos inválidos", "Título y plataforma son obligatorios.");
                return;
            }
            if (!decimal.TryParse(PriceBox.Text.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal price) || price <= 0 ||
                !int.TryParse(StockBox.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int stock))
            {
                await Dialogs.Info(XamlRoot, "Datos inválidos", "El precio debe ser mayor a 0 (ej. 899.50) y el stock un entero mayor o igual a 0.");
                return;
            }

            if (_pendingCoverPath == null)
            {
                await Dialogs.Info(XamlRoot, "Falta la carátula", "Elige la carátula del videojuego con el botón \"Elegir carátula...\".");
                return;
            }

            string coverFile;
            try
            {
                coverFile = DataStore.ImportCover(_pendingCoverPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                await Dialogs.Info(XamlRoot, "Error", $"No se pudo guardar la carátula:\n{ex.Message}");
                return;
            }

            var game = new Videogame
            {
                Id = DataStore.NextGameId(),
                Title = title,
                Platform = platform,
                Price = price,
                Stock = stock,
                CoverFile = coverFile
            };
            DataStore.Games.Add(game);
            DataStore.Save();
            LoadCatalog(game.Id);

            TitleBox.Text = "";
            PriceBox.Text = "";
            StockBox.Text = "";
            ClearPendingCover();
        }

        private async void PickCover_Click(object sender, RoutedEventArgs e)
        {
            string path = await CoverPicker.PickAsync();
            if (path == null) return;

            if (!DataStore.IsValidCoverFile(path, out string error))
            {
                await Dialogs.Info(XamlRoot, "Imagen no válida", error);
                return;
            }

            _pendingCoverPath = path;
            CoverPreviewBrush.ImageSource = new BitmapImage(new Uri(path)) { DecodePixelWidth = 200 };
            CoverNameText.Text = System.IO.Path.GetFileName(path);
        }

        private void ClearPendingCover()
        {
            _pendingCoverPath = null;
            CoverPreviewBrush.ImageSource = null;
            CoverNameText.Text = "Ninguna imagen seleccionada";
        }

        private async void ChangeCover_Click(object sender, RoutedEventArgs e)
        {
            if (CatalogList.SelectedItem is not Videogame game)
            {
                await Dialogs.Info(XamlRoot, "Aviso", "Selecciona un videojuego para cambiar su carátula.");
                return;
            }

            string path = await CoverPicker.PickAsync();
            if (path == null) return;

            if (!DataStore.IsValidCoverFile(path, out string error))
            {
                await Dialogs.Info(XamlRoot, "Imagen no válida", error);
                return;
            }

            try
            {
                string oldCover = game.CoverFile;
                game.CoverFile = DataStore.ImportCover(path);
                DataStore.Save();
                DataStore.DeleteCoverIfUnused(oldCover);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                await Dialogs.Info(XamlRoot, "Error", $"No se pudo guardar la carátula:\n{ex.Message}");
                return;
            }

            LoadCatalog(game.Id);
        }

        private async void RemoveGame_Click(object sender, RoutedEventArgs e)
        {
            if (CatalogList.SelectedItem is not Videogame game)
            {
                await Dialogs.Info(XamlRoot, "Aviso", "Selecciona un videojuego.");
                return;
            }

            if (!await Dialogs.Confirm(XamlRoot, "Confirmar", $"¿Eliminar \"{game.Title}\" del catálogo?")) return;

            DataStore.Games.Remove(game);
            DataStore.Save();
            DataStore.DeleteCoverIfUnused(game.CoverFile);
            LoadCatalog();
        }

        private async void IncreaseStock_Click(object sender, RoutedEventArgs e)
        {
            if (CatalogList.SelectedItem is not Videogame game)
            {
                await Dialogs.Info(XamlRoot, "Aviso", "Selecciona un videojuego para aumentar su stock.");
                return;
            }

            double value = StockIncreaseBox.Value;
            if (double.IsNaN(value) || value < 1 || value != Math.Floor(value))
            {
                await Dialogs.Info(XamlRoot, "Datos inválidos", "Escribe una cantidad entera mayor a 0.");
                return;
            }

            if (!DataStore.IncreaseStock(game.Id, (int)value))
            {
                await Dialogs.Info(XamlRoot, "Error", "No se pudo actualizar el stock.");
                return;
            }

            LoadCatalog(game.Id);
            StockIncreaseBox.Value = 1;
            await Dialogs.Info(XamlRoot, "Stock", $"Stock actualizado: \"{game.Title}\" ahora tiene {game.Stock} unidades.");
        }

        private void Price_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            args.Cancel = !Input.IsDecimalText(args.NewText);
        }

        private void Stock_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            args.Cancel = !Input.IsDigitsText(args.NewText);
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LoginPage));
            Frame.BackStack.Clear();
        }
    }
}
