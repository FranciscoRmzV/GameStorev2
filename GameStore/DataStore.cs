using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace GameStore
{
    public static class DataStore
    {
        private static readonly string Dir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GameStore");
        private static readonly string UsersFile = Path.Combine(Dir, "users.json");
        private static readonly string GamesFile = Path.Combine(Dir, "games.json");
        private static readonly string CustomCoversDir = Path.Combine(Dir, "Covers");
        private static readonly string BundledCoversDir =
            Path.Combine(AppContext.BaseDirectory, "Assets", "Covers");
        public static readonly string[] CoverExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".webp" };
        private const long MaxCoverBytes = 10 * 1024 * 1024;
        private static readonly string TicketsDir = Path.Combine(Dir, "Tickets");
        private static readonly JsonSerializerOptions Opt = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static List<User> Users { get; private set; }
        public static List<Videogame> Games { get; private set; }

        static DataStore()
        {
            Directory.CreateDirectory(Dir);

            Users = File.Exists(UsersFile)
                ? JsonSerializer.Deserialize<List<User>>(File.ReadAllText(UsersFile))
                : new List<User>();

            Games = File.Exists(GamesFile)
                ? JsonSerializer.Deserialize<List<Videogame>>(File.ReadAllText(GamesFile))
                : new List<Videogame>();

            if (!Users.Any(u => u.Role == Role.Admin))
                Users.Add(new User { Username = "admin", PasswordHash = Hash("admin123"), Role = Role.Admin });

            if (Games.Count == 0)
            {
                Games.Add(new Videogame { Id = 1, Title = "Halo Infinite", CoverFile = "halo-infinite.png", Platform = "Xbox Series X|S", Price = 899, Stock = 10 });
                Games.Add(new Videogame { Id = 2, Title = "Forza Horizon 5", CoverFile = "forza-horizon-5.png", Platform = "Xbox Series X|S", Price = 1099, Stock = 8 });
                Games.Add(new Videogame { Id = 3, Title = "Starfield", CoverFile = "starfield.png", Platform = "Xbox Series X|S", Price = 1499, Stock = 6 });
                Games.Add(new Videogame { Id = 4, Title = "Gears 5", CoverFile = "gears-5.png", Platform = "Xbox Series X|S", Price = 599, Stock = 12 });
                Games.Add(new Videogame { Id = 5, Title = "Sea of Thieves", CoverFile = "sea-of-thieves.png", Platform = "Xbox Series X|S", Price = 699, Stock = 9 });
                Games.Add(new Videogame { Id = 6, Title = "Minecraft", CoverFile = "minecraft.png", Platform = "Xbox Series X|S", Price = 599, Stock = 15 });
            }
            AssignBundledCovers();
            AddMissingBundledGames();
            Save();
        }

        private static void AddMissingBundledGames()
        {
            var extras = new[]
            {
                new Videogame { Title = "Halo: Campaign Evolved", CoverFile = "halo-campaign-evolved.png", Platform = "Xbox Series X|S", Price = 1299, Stock = 8 },
                new Videogame { Title = "God of War Ragnarök", CoverFile = "god-of-war-ragnarok.png", Platform = "PlayStation 5", Price = 1199, Stock = 7 },
                new Videogame { Title = "The Legend of Zelda: Breath of the Wild", CoverFile = "the-legend-of-zelda-breath-of-the-wild.png", Platform = "Nintendo Switch", Price = 1399, Stock = 5 }
            };
            foreach (var game in extras)
            {
                if (Games.Any(g => g.Title.Equals(game.Title, StringComparison.OrdinalIgnoreCase))) continue;
                game.Id = NextGameId();
                Games.Add(game);
            }
        }

        private static void AssignBundledCovers()
        {
            foreach (var game in Games.Where(g => string.IsNullOrWhiteSpace(g.CoverFile)))
            {
                string candidate = Slug(game.Title) + ".png";
                if (File.Exists(Path.Combine(BundledCoversDir, candidate)))
                    game.CoverFile = candidate;
            }
        }

        private static string Slug(string text) =>
            Regex.Replace((text ?? "").ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');


        public static string ResolveCoverPath(string coverFile)
        {
            if (!string.IsNullOrWhiteSpace(coverFile))
            {
                string name = Path.GetFileName(coverFile);
                string custom = Path.Combine(CustomCoversDir, name);
                if (File.Exists(custom)) return custom;

                string bundled = Path.Combine(BundledCoversDir, name);
                if (File.Exists(bundled)) return bundled;
            }
            return Path.Combine(BundledCoversDir, "default.png");
        }

        public static ImageSource LoadCover(string coverFile, int decodeWidth)
        {
            string path = ResolveCoverPath(coverFile);
            if (!File.Exists(path)) return null;
            return new BitmapImage(new Uri(path)) { DecodePixelWidth = decodeWidth };
        }

        public static bool IsValidCoverFile(string path, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                error = "No se encontró el archivo de imagen.";
                return false;
            }
            if (!CoverExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
            {
                error = "Formato no válido. Usa PNG, JPG, BMP o WEBP.";
                return false;
            }
            if (new FileInfo(path).Length > MaxCoverBytes)
            {
                error = "La imagen pesa más de 10 MB.";
                return false;
            }
            return true;
        }

        public static string ImportCover(string sourcePath)
        {
            Directory.CreateDirectory(CustomCoversDir);
            string fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(sourcePath).ToLowerInvariant();
            File.Copy(sourcePath, Path.Combine(CustomCoversDir, fileName));
            return fileName;
        }

        public static void DeleteCoverIfUnused(string coverFile)
        {
            if (string.IsNullOrWhiteSpace(coverFile) || Games.Any(g => g.CoverFile == coverFile)) return;
            try
            {
                string path = Path.Combine(CustomCoversDir, Path.GetFileName(coverFile));
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }

        public static void Save()
        {
            File.WriteAllText(UsersFile, JsonSerializer.Serialize(Users, Opt));
            File.WriteAllText(GamesFile, JsonSerializer.Serialize(Games, Opt));
        }

        public static string Hash(string text)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(text)));
        }

        public static User Authenticate(string username, string password) =>
            Users.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
                u.PasswordHash == Hash(password));

        public static bool RegisterClient(string username, string password)
        {
            if (Users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                return false;

            Users.Add(new User { Username = username, PasswordHash = Hash(password), Role = Role.Client });
            Save();
            return true;
        }

        public static int NextGameId() => Games.Count == 0 ? 1 : Games.Max(g => g.Id) + 1;

        public static bool IncreaseStock(int id, int quantity)
        {
            var game = Games.FirstOrDefault(g => g.Id == id);
            if (game == null || quantity <= 0) return false;
            game.Stock += quantity;
            Save();
            return true;
        }

        public static string SaveTicket(Ticket ticket)
        {
            Directory.CreateDirectory(TicketsDir);
            string path = Path.Combine(TicketsDir, ticket.Folio + ".json");
            File.WriteAllText(path, JsonSerializer.Serialize(ticket, Opt), Encoding.UTF8);
            return path;
        }
    }
}
