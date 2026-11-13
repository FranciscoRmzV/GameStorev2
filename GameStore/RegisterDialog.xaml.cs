using Microsoft.UI.Xaml.Controls;

namespace GameStore
{
    public sealed partial class RegisterDialog : ContentDialog
    {
        public RegisterDialog()
        {
            InitializeComponent();
        }

        private void Create_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            string username = UsernameBox.Text.Trim();

            if (username.Length < 3 || PasswordInput.Password.Length < 4)
            {
                ErrorText.Text = "Usuario mínimo 3 caracteres y contraseña mínimo 4.";
                args.Cancel = true;
                return;
            }
            if (PasswordInput.Password != ConfirmInput.Password)
            {
                ErrorText.Text = "Las contraseñas no coinciden.";
                args.Cancel = true;
                return;
            }
            if (!DataStore.RegisterClient(username, PasswordInput.Password))
            {
                ErrorText.Text = "Ese usuario ya existe.";
                args.Cancel = true;
            }
        }
    }
}
