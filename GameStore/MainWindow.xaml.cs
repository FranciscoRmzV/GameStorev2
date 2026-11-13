using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace GameStore
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Title = "Game Store";
            AppWindow.Resize(new SizeInt32(1280, 860));
            RootFrame.Navigate(typeof(LoginPage));
        }
    }
}
