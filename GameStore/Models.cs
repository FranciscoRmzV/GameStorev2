using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.UI.Xaml.Media;

namespace GameStore
{
    public enum Role { Admin, Client }

    public static class Currency
    {
        private static readonly CultureInfo Mx = new("es-MX");

        public static string Format(decimal value) => value.ToString("C", Mx);
    }

    public static class Input
    {
        public static bool IsDecimalText(string text)
        {
            int dots = 0;
            foreach (char c in text)
            {
                if (c == '.') dots++;
                else if (!char.IsDigit(c)) return false;
            }
            return dots <= 1;
        }

        public static bool IsDigitsText(string text) => text.All(char.IsDigit);
    }

    public class User
    {
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public Role Role { get; set; }
    }

    public class Videogame
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Platform { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }

        private string _coverFile;
        private ImageSource _cover;

        public string CoverFile
        {
            get => _coverFile;
            set { _coverFile = value; _cover = null; }
        }

        [JsonIgnore]
        public ImageSource Cover => _cover ??= DataStore.LoadCover(CoverFile, 320);

        [JsonIgnore]
        public string PriceText => Currency.Format(Price);

        [JsonIgnore]
        public string StockText => $"Stock: {Stock}";
    }

    public class TicketLine
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal => UnitPrice * Quantity;

        [JsonIgnore]
        public ImageSource Cover =>
            DataStore.Games.FirstOrDefault(g => g.Id == Id)?.Cover ?? DataStore.LoadCover(null, 120);

        [JsonIgnore]
        public string DetailText => $"{Quantity} x {Currency.Format(UnitPrice)}";

        [JsonIgnore]
        public string SubtotalText => Currency.Format(Subtotal);
    }

    public class Ticket
    {
        public string Folio { get; set; }
        public DateTime Date { get; set; }
        public string Client { get; set; }
        public List<TicketLine> Lines { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public decimal Payment { get; set; }
        public decimal Change { get; set; }
    }
}
