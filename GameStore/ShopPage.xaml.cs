using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

namespace GameStore
{
    public sealed partial class ShopPage : Page
    {
        private const decimal TaxRate = 0.16m;
        private const decimal DiscountThreshold = 2000m;
        private const decimal DiscountRate = 0.10m;

        private readonly List<TicketLine> _cart = new();
        private User _client;

        public ShopPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _client = e.Parameter as User;
            ClientText.Text = $"Cliente: {_client?.Username}";
            LoadCatalog();
            RefreshCart();
        }

        private void LoadCatalog()
        {
            CatalogList.ItemsSource = DataStore.Games.Where(g => g.Stock > 0).ToList();
        }

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            await AddSelected();
        }

        private async void CatalogList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            await AddSelected();
        }

        private async Task AddSelected()
        {
            if (CatalogList.SelectedItem is not Videogame game)
            {
                await Dialogs.Info(XamlRoot, "Aviso", "Selecciona un videojuego.");
                return;
            }

            double value = QuantityBox.Value;
            if (double.IsNaN(value) || value < 1 || value != Math.Floor(value))
            {
                await Dialogs.Info(XamlRoot, "Datos inválidos", "Escribe una cantidad entera mayor a 0.");
                return;
            }

            int quantity = (int)value;
            var line = _cart.FirstOrDefault(l => l.Id == game.Id);
            int inCart = line?.Quantity ?? 0;

            if (inCart + quantity > game.Stock)
            {
                await Dialogs.Info(XamlRoot, "Sin stock", $"Solo hay {game.Stock - inCart} unidades disponibles de este juego.");
                return;
            }

            if (line == null)
                _cart.Add(new TicketLine { Id = game.Id, Name = game.Title, UnitPrice = game.Price, Quantity = quantity });
            else
                line.Quantity += quantity;

            QuantityBox.Value = 1;
            RefreshCart();
        }

        private async void Remove_Click(object sender, RoutedEventArgs e)
        {
            if (CartList.SelectedItem is not TicketLine line)
            {
                await Dialogs.Info(XamlRoot, "Aviso", "Selecciona un juego del carrito.");
                return;
            }
            _cart.RemoveAll(l => l.Id == line.Id);
            RefreshCart();
        }

        private async void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (_cart.Count == 0) return;
            if (!await Dialogs.Confirm(XamlRoot, "Confirmar", "¿Deseas vaciar el carrito?")) return;
            _cart.Clear();
            RefreshCart();
        }

        private async void Checkout_Click(object sender, RoutedEventArgs e)
        {
            if (_cart.Count == 0)
            {
                await Dialogs.Info(XamlRoot, "Aviso", "El carrito está vacío.");
                return;
            }

            if (!TryReadPayment(out decimal payment)) return;

            Calculate(out decimal subtotal, out decimal discount, out decimal tax, out decimal total);

            if (payment < total)
            {
                PaymentError.Text = $"El pago no cubre el total ({Currency.Format(total)}).";
                return;
            }

            foreach (var line in _cart)
            {
                var game = DataStore.Games.FirstOrDefault(g => g.Id == line.Id);
                if (game == null || game.Stock < line.Quantity)
                {
                    await Dialogs.Info(XamlRoot, "Error", $"Stock insuficiente o juego no disponible: {line.Name}.");
                    LoadCatalog();
                    return;
                }
            }

            foreach (var line in _cart)
                DataStore.Games.First(g => g.Id == line.Id).Stock -= line.Quantity;

            DateTime now = DateTime.Now;
            var ticket = new Ticket
            {
                Folio = $"XB-{now:yyyyMMddHHmmss}",
                Date = now,
                Client = _client.Username,
                Lines = _cart.Select(l => new TicketLine
                {
                    Id = l.Id, Name = l.Name, UnitPrice = l.UnitPrice, Quantity = l.Quantity
                }).ToList(),
                Subtotal = subtotal,
                Discount = discount,
                Tax = tax,
                Total = total,
                Payment = payment,
                Change = payment - total
            };

            TicketBox.Text = BuildTicketText(ticket);

            try
            {
                DataStore.Save();
                string path = DataStore.SaveTicket(ticket);
                await Dialogs.Info(XamlRoot, "Gracias", $"¡Compra realizada!\n\nTicket guardado en:\n{path}");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                await Dialogs.Info(XamlRoot, "Error", $"No se pudo guardar el archivo JSON:\n{ex.Message}");
            }

            _cart.Clear();
            PaymentBox.Text = "";
            PaymentError.Text = "";
            LoadCatalog();
            RefreshCart();
        }

        private void Payment_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            args.Cancel = !Input.IsDecimalText(args.NewText);
        }

        private void Payment_LostFocus(object sender, RoutedEventArgs e)
        {
            if (PaymentBox.Text.Trim() != "") TryReadPayment(out _);
        }

        private bool TryReadPayment(out decimal payment)
        {
            if (!decimal.TryParse(PaymentBox.Text.Trim(), NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out payment) || payment <= 0)
            {
                PaymentError.Text = "Escribe un monto numérico mayor a 0 (ej. 1500.50).";
                return false;
            }
            PaymentError.Text = "";
            return true;
        }

        private void Calculate(out decimal subtotal, out decimal discount, out decimal tax, out decimal total)
        {
            subtotal = _cart.Sum(l => l.Subtotal);
            discount = subtotal >= DiscountThreshold ? Math.Round(subtotal * DiscountRate, 2) : 0m;
            decimal taxable = subtotal - discount;
            tax = Math.Round(taxable * TaxRate, 2);
            total = taxable + tax;
        }

        private void RefreshCart()
        {
            CartList.ItemsSource = _cart.ToList();

            Calculate(out decimal subtotal, out decimal discount, out decimal tax, out decimal total);
            SubtotalText.Text = $"Subtotal:   {Currency.Format(subtotal)}";
            DiscountText.Text = $"Descuento 10% (desde {Currency.Format(DiscountThreshold)}):   -{Currency.Format(discount)}";
            TaxText.Text = $"IVA 16%:   {Currency.Format(tax)}";
            TotalText.Text = $"TOTAL:   {Currency.Format(total)}";
        }

        private static string BuildTicketText(Ticket t)
        {
            const int width = 44;
            var sb = new StringBuilder();
            string equals = new('=', width);
            string dashes = new('-', width);

            sb.AppendLine(Center("GAME STORE", width));
            sb.AppendLine(Center("Ticket de compra", width));
            sb.AppendLine(equals);
            sb.AppendLine($"Folio:   {t.Folio}");
            sb.AppendLine($"Fecha:   {t.Date:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine($"Cliente: {Cut(t.Client, width - 9)}");
            sb.AppendLine(dashes);

            foreach (var l in t.Lines)
            {
                sb.AppendLine(Cut(l.Name, width));
                sb.AppendLine(Row($"  {l.DetailText}", l.Subtotal, width));
            }

            sb.AppendLine(dashes);
            sb.AppendLine(Row("Subtotal", t.Subtotal, width));
            sb.AppendLine(Row("Descuento", -t.Discount, width));
            sb.AppendLine(Row("IVA 16%", t.Tax, width));
            sb.AppendLine(Row("TOTAL", t.Total, width));
            sb.AppendLine(dashes);
            sb.AppendLine(Row("Pago", t.Payment, width));
            sb.AppendLine(Row("Cambio", t.Change, width));
            sb.AppendLine(equals);
            sb.AppendLine(Center("¡Gracias por tu compra!", width));
            return sb.ToString();
        }

        private static string Row(string label, decimal value, int width) =>
            label.PadRight(width - 14) + Currency.Format(value).PadLeft(14);

        private static string Cut(string text, int max) => text.Length <= max ? text : text[..max];

        private static string Center(string text, int width) =>
            text.PadLeft((width + text.Length) / 2).PadRight(width);

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LoginPage));
            Frame.BackStack.Clear();
        }
    }
}
