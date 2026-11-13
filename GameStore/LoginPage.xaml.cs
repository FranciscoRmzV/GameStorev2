using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.System;

namespace GameStore
{
    public sealed partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
            BuildCoversStrip();
        }

        private void BuildCoversStrip()
        {
            double[] angles = { -7, 0, 7 };
            double[] lifts = { 0, -22, 0 };
            string[] covers = { "halo-campaign-evolved.png", "god-of-war-ragnarok.png", "the-legend-of-zelda-breath-of-the-wild.png" };
            for (int i = 0; i < covers.Length; i++)
            {
                CoversStrip.Children.Add(new Border
                {
                    Width = 180,
                    Height = 270,
                    CornerRadius = new CornerRadius(12),
                    BorderBrush = (Brush)Application.Current.Resources["GoldBrush"],
                    BorderThickness = new Thickness(2),
                    Margin = new Thickness(-13, lifts[i], -13, 0),
                    RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5),
                    RenderTransform = new RotateTransform { Angle = angles[i] },
                    Background = new ImageBrush { ImageSource = DataStore.LoadCover(covers[i], 340), Stretch = Stretch.UniformToFill }
                });
            }
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            await TryLogin();
        }

        private async void PasswordInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter) await TryLogin();
        }

        private async Task TryLogin()
        {
            string username = UsernameBox.Text.Trim();
            if (username == "" || PasswordInput.Password == "")
            {
                await Dialogs.Info(XamlRoot, "Aviso", "Escribe usuario y contraseña.");
                return;
            }

            var user = DataStore.Authenticate(username, PasswordInput.Password);
            if (user == null)
            {
                await Dialogs.Info(XamlRoot, "Error", "Usuario o contraseña incorrectos.");
                PasswordInput.Password = "";
                return;
            }

            Frame.Navigate(user.Role == Role.Admin ? typeof(AdminPage) : typeof(ShopPage), user);
            Frame.BackStack.Clear();
        }

        private async void Register_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new RegisterDialog { XamlRoot = XamlRoot };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                await Dialogs.Info(XamlRoot, "Listo", "Cuenta creada. Ya puedes iniciar sesión.");
        }
    }
}
