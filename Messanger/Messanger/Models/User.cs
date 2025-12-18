using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;

namespace Messenger.Models
{
    public enum UserStatus
    {
        Online,
        Offline,
        DoNotDisturb
    }

    public class User
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("username")]
        public string Username { get; set; } = string.Empty;

        [JsonProperty("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonProperty("email")]
        public string Email { get; set; } = string.Empty;

        [JsonProperty("passwordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        [JsonProperty("avatarUrl")]
        public string AvatarUrl { get; set; } = string.Empty;

        [JsonProperty("status")]
        public UserStatus Status { get; set; } = UserStatus.Offline;

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonProperty("lastSeen")]
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;

        [JsonProperty("gender")]
        public string Gender { get; set; } = string.Empty;

        [JsonProperty("birthDate")]
        public string BirthDate { get; set; } = string.Empty;

        [JsonProperty("languages")]
        public string Languages { get; set; } = string.Empty;

        [JsonProperty("country")]
        public string Country { get; set; } = string.Empty;

        [JsonProperty("city")]
        public string City { get; set; } = string.Empty;

        [JsonProperty("bio")]
        public string Bio { get; set; } = string.Empty;

        // Кэшированное изображение аватара
        [JsonIgnore]
        public BitmapImage? AvatarImage { get; set; }

        // Свойство для определения, есть ли аватар
        [JsonIgnore]
        public bool HasAvatar => !string.IsNullOrEmpty(AvatarUrl);

        // Инициалы пользователя (для дефолтного аватара)
        [JsonIgnore]
        public string Initials
        {
            get
            {
                if (string.IsNullOrEmpty(DisplayName))
                    return "U";

                var parts = DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    return "U";

                if (parts.Length == 1)
                    return parts[0].Substring(0, 1).ToUpper();

                return $"{parts[0].Substring(0, 1)}{parts[^1].Substring(0, 1)}".ToUpper();
            }
        }

        // Событие для уведомления об изменении аватара
        public static event EventHandler? AvatarChanged;


        [JsonProperty("isAdmin")]
        public bool IsAdmin { get; set; } = false;

        // Статическое свойство для текущего пользователя
        public static User? CurrentUser { get; set; }


        [JsonProperty("uid")]
        public string Uid { get; set; } = string.Empty; // Firebase Auth UID

        [JsonProperty("phoneNumber")]
        public string PhoneNumber { get; set; } = string.Empty;

        [JsonProperty("emailVerified")]
        public bool EmailVerified { get; set; } = false;

        [JsonIgnore]
        public string? IdToken { get; set; }



        // Загружает аватар (из URL или файла)
        public async Task LoadAvatarAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(AvatarUrl))
                {
                    AvatarImage = null;
                    return;
                }

                if (AvatarUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    // Загружаем из интернета
                    await LoadAvatarFromUrlAsync(AvatarUrl);
                }
                else if (File.Exists(AvatarUrl))
                {
                    // Загружаем локальный файл
                    LoadAvatarFromFile(AvatarUrl);
                }
                else
                {
                    // Файл не найден
                    AvatarImage = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadAvatarAsync error: {ex.Message}");
                AvatarImage = null;
            }
        }

        private async Task LoadAvatarFromUrlAsync(string url)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(url);
                bitmap.DecodePixelWidth = 100; // Оптимизация размера
                bitmap.EndInit();
                bitmap.Freeze();

                AvatarImage = bitmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadAvatarFromUrlAsync error: {ex.Message}");
                AvatarImage = null;
            }
        }

        private void LoadAvatarFromFile(string filePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(filePath);
                bitmap.DecodePixelWidth = 100; // Оптимизация размера
                bitmap.EndInit();
                bitmap.Freeze();

                AvatarImage = bitmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadAvatarFromFile error: {ex.Message}");
                AvatarImage = null;
            }
        }

        public void CreateDefaultAvatar()
        {
            try
            {
                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    // Фон (синий градиент)
                    var gradient = new LinearGradientBrush(
                        Color.FromRgb(0, 120, 215),
                        Color.FromRgb(0, 90, 160),
                        new System.Windows.Point(0, 0),
                        new System.Windows.Point(1, 1));

                    drawingContext.DrawRectangle(
                        gradient,
                        null,
                        new System.Windows.Rect(0, 0, 100, 100));

                    // Инициалы
                    var text = new FormattedText(
                        Initials,
                        System.Globalization.CultureInfo.CurrentCulture,
                        System.Windows.FlowDirection.LeftToRight,
                        new Typeface("Arial Bold"),
                        36,
                        Brushes.White,
                        System.Windows.Media.VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

                    drawingContext.DrawText(
                        text,
                        new System.Windows.Point(50 - text.Width / 2, 32 - text.Height / 2));
                }

                // Преобразуем в BitmapImage
                var renderBitmap = new RenderTargetBitmap(100, 100, 96, 96, PixelFormats.Pbgra32);
                renderBitmap.Render(drawingVisual);

                var bitmapImage = new BitmapImage();
                var bitmapEncoder = new PngBitmapEncoder();
                bitmapEncoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                using (var stream = new MemoryStream())
                {
                    bitmapEncoder.Save(stream);
                    stream.Seek(0, SeekOrigin.Begin);

                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = stream;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();
                }

                AvatarImage = bitmapImage;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CreateDefaultAvatar error: {ex.Message}");
                AvatarImage = null;
            }
        }

        // Метод для обновления аватара (вызывается при изменении)
        public void UpdateAvatar(string newAvatarUrl)
        {
            AvatarUrl = newAvatarUrl;
            AvatarImage = null; // Сбрасываем кэш

            // Уведомляем об изменении
            AvatarChanged?.Invoke(this, EventArgs.Empty);
        }

        // Метод для принудительной перезагрузки аватара
        public async Task RefreshAvatarAsync()
        {
            AvatarImage = null;
            await LoadAvatarAsync();

            // Уведомляем об изменении
            AvatarChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
