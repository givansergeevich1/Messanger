using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Messenger.Models;
using Messenger.Services;
using Messenger.Themes;
using Messenger.Views;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Messenger.ViewModels
{
    public partial class ProfileViewModel : ObservableObject
    {
        private readonly FirebaseService _firebaseService;
        private readonly LocalStorageService _localStorage;
        private readonly NavigationService _navigationService;
        private readonly ImgBBService _imgBBService;

        [ObservableProperty]
        private string _avatarInfo = "Аватар не загружен";

        [ObservableProperty]
        private string _avatarStatus = "Неизвестно";

        [ObservableProperty]
        private SolidColorBrush _avatarStatusColor = new SolidColorBrush(Color.FromRgb(200, 200, 200));

        [ObservableProperty]
        private bool _isAvatarDefault = true;

        [ObservableProperty]
        private string _selectedAvatarPath = string.Empty;

        [ObservableProperty]
        private bool _hasAvatarChanges = false;

        [ObservableProperty]
        private bool _isUploadingAvatar = false;

        [ObservableProperty]
        private User? _currentUser;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _displayName = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private BitmapImage? _avatarImage;

        [ObservableProperty]
        private string _originalUsername = string.Empty;

        [ObservableProperty]
        private string _originalEmail = string.Empty;

        [ObservableProperty]
        private string _originalDisplayName = string.Empty;

        [ObservableProperty]
        private bool _isUsernameAvailable = true;

        [ObservableProperty]
        private bool _isEmailAvailable = true;

        [ObservableProperty]
        private bool _isCheckingAvailability = false;

        [ObservableProperty]
        private string _selectedGender = "Мужской";

        [ObservableProperty]
        private DateTime? _birthDate = DateTime.Now.AddYears(-20);

        [ObservableProperty]
        private string _languages = "Русский";

        [ObservableProperty]
        private string _country = "Россия";

        [ObservableProperty]
        private string _city = "Москва";

        [ObservableProperty]
        private string _bio = string.Empty;

        [ObservableProperty]
        private UserStatus _selectedStatus = UserStatus.Online;

        [ObservableProperty]
        private bool _isEditing = false;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _errorMessage = string.Empty;


        [ObservableProperty]
        private bool _isLightTheme;

        [ObservableProperty]
        private bool _isDarkTheme;

        [ObservableProperty]
        private bool _soundNotifications = true;

        [ObservableProperty]
        private bool _popupNotifications = true;

        [ObservableProperty]
        private bool _autoSaveFiles = false;

        // Цвет фона для превью темы
        [ObservableProperty]
        private SolidColorBrush _themePreviewBackground = new SolidColorBrush(Color.FromRgb(248, 249, 250));

        [ObservableProperty]
        private SolidColorBrush _themePreviewForeground = new SolidColorBrush(Color.FromRgb(33, 33, 33));

        // Доступные статусы для ComboBox
        public ObservableCollection<UserStatus> AvailableStatuses { get; } = new()
        {
            UserStatus.Online,
            UserStatus.Offline,
            UserStatus.DoNotDisturb
        };

        // Доступные гендеры для ComboBox
        public ObservableCollection<string> AvailableGenders { get; } = new()
        {
            "Мужской",
            "Женский"
        };

        public ProfileViewModel(
            FirebaseService firebaseService,
            LocalStorageService localStorage,
            NavigationService navigationService)
        {
            _firebaseService = firebaseService;
            _localStorage = localStorage;
            _navigationService = navigationService;
            _imgBBService = new ImgBBService();

            InitializeUserData();
            InitializeThemeSettings();

            LoadAvatar();

            // Инициализируем информацию об аватаре
            UpdateAvatarInfo();
        }
        private void InitializeThemeSettings()
        {
            // Загружаем текущую тему
            IsDarkTheme = ThemeManager.IsDarkTheme;
            IsLightTheme = !IsDarkTheme;

            // Обновляем превью
            UpdateThemePreview();

            // Подписываемся на изменение темы для обновления превью
            ThemeManager.ThemeChanged += (s, e) =>
            {
                IsDarkTheme = ThemeManager.IsDarkTheme;
                IsLightTheme = !IsDarkTheme;
                UpdateThemePreview();
            };
        }


        [RelayCommand]
        private async Task RefreshUserAvatar()
        {
            try
            {
                Console.WriteLine("=== REFRESH USER AVATAR COMMAND ===");

                if (CurrentUser == null) return;

                // 1. Получаем свежие данные из Firebase
                var freshUser = await _firebaseService.GetUserByUidAsync(CurrentUser.Uid);
                if (freshUser == null)
                {
                    Console.WriteLine("❌ Failed to get fresh user from Firebase");
                    return;
                }

                Console.WriteLine($"Old AvatarUrl: {CurrentUser.AvatarUrl}");
                Console.WriteLine($"New AvatarUrl: {freshUser.AvatarUrl}");

                // 2. Обновляем URL (если изменился)
                if (CurrentUser.AvatarUrl != freshUser.AvatarUrl)
                {
                    CurrentUser.AvatarUrl = freshUser.AvatarUrl;

                    // Сохраняем локально
                    _localStorage.SaveUser(CurrentUser);
                }

                // 3. Перезагружаем аватар
                if (string.IsNullOrEmpty(CurrentUser.AvatarUrl))
                {
                    // Нет аватара - создаем дефолтный
                    CurrentUser.CreateDefaultAvatar();
                }
                else
                {
                    // Загружаем реальный аватар
                    await CurrentUser.LoadAvatarAsync();
                }

                // 4. Уведомляем UI
                OnPropertyChanged(nameof(CurrentUser));

                Console.WriteLine("✅ Avatar refreshed in MainWindow");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ RefreshUserAvatar error: {ex.Message}");
            }
        }
        private async void UpdateAvatarInMainWindow()
        {
            try
            {
                Console.WriteLine("=== UPDATE AVATAR IN MAIN WINDOW ===");

                var mainWindow = Application.Current.Windows
                    .OfType<MainWindow>()
                    .FirstOrDefault();

                if (mainWindow != null && mainWindow.DataContext is MainViewModel mainViewModel)
                {
                    await RefreshUserAvatar();
                }

                if (User.CurrentUser != null && CurrentUser != null)
                {
                    User.CurrentUser.AvatarUrl = CurrentUser.AvatarUrl;
                    if (string.IsNullOrEmpty(User.CurrentUser.AvatarUrl))
                    {
                        User.CurrentUser.CreateDefaultAvatar();
                    }
                    else
                    {
                        await User.CurrentUser.LoadAvatarAsync();
                    }

                    Console.WriteLine($"✅ Static User.CurrentUser avatar updated: {User.CurrentUser.AvatarUrl}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ UpdateAvatarInMainWindow error: {ex.Message}");
            }
        }
        private void UpdateThemePreview()
        {
            if (ThemeManager.IsDarkTheme)
            {
                ThemePreviewBackground = new SolidColorBrush(Color.FromRgb(40, 40, 45));
                ThemePreviewForeground = new SolidColorBrush(Colors.White);
            }
            else
            {
                ThemePreviewBackground = new SolidColorBrush(Color.FromRgb(248, 249, 250));
                ThemePreviewForeground = new SolidColorBrush(Color.FromRgb(33, 33, 33));
            }
        }

        [RelayCommand]
        private void ApplyLightTheme()
        {
            IsLightTheme = true;
            IsDarkTheme = false;
            ThemeManager.ApplyLightTheme();
            UpdateThemePreview();

            // Сообщаем об изменении темы
            OnThemeChanged();
        }

        [RelayCommand]
        private void ApplyDarkTheme()
        {
            IsDarkTheme = true;
            IsLightTheme = false;
            ThemeManager.ApplyDarkTheme();
            UpdateThemePreview();

            // Сообщаем об изменении темы
            OnThemeChanged();
        }

        [RelayCommand]
        private void ToggleTheme()
        {
            if (IsDarkTheme)
                ApplyLightTheme();
            else
                ApplyDarkTheme();
        }

        [RelayCommand]
        private async Task SaveThemeSettings()
        {
            try
            {
                // Тема уже сохранена в ThemeManager.ApplyDarkTheme/LightTheme

                // Сохраняем другие настройки
                await SaveUserSettings();

                MessageBox.Show("Настройки темы сохранены", "Успех");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка сохранения настроек: {ex.Message}";
            }
        }

        private async Task SaveUserSettings()
        {
            try
            {
                // Простое сохранение настроек в файл
                var settings = new
                {
                    SoundNotifications = SoundNotifications,
                    PopupNotifications = PopupNotifications,
                    AutoSaveFiles = AutoSaveFiles,
                    Theme = ThemeManager.IsDarkTheme ? "Dark" : "Light",
                    SavedAt = DateTime.UtcNow
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
                var path = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Messenger",
                    "user_settings.json");

                var directory = System.IO.Path.GetDirectoryName(path);
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                await System.IO.File.WriteAllTextAsync(path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения настроек: {ex.Message}");
            }
        }

        private void OnThemeChanged()
        {
            // Можно вызвать событие для обновления UI в реальном времени
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Обновляем главное окно если оно открыто
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    // Вызываем метод обновления темы в MainWindow
                    mainWindow.UpdateThemeColors();
                }

                // Показываем сообщение
                MessageBox.Show($"Тема изменена на: {(ThemeManager.IsDarkTheme ? "Темную" : "Светлую")}\n" +
                               "Некоторые элементы интерфейса могут потребовать перезапуска приложения.",
                               "Тема", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        private void InitializeUserData()
        {
            CurrentUser = User.CurrentUser ?? _localStorage.GetCurrentUser();

            if (CurrentUser != null)
            {
                LoadUserData();
                OriginalUsername = CurrentUser.Username;
                OriginalEmail = CurrentUser.Email;
                OriginalDisplayName = CurrentUser.DisplayName;
            }
            else
            {
                // Если пользователь не найден, возвращаемся на главный экран
                _navigationService.NavigateToMain();
            }
        }

        private void LoadUserData()
        {
            Username = CurrentUser!.Username;
            DisplayName = CurrentUser.DisplayName;
            Email = CurrentUser.Email;
            SelectedGender = string.IsNullOrEmpty(CurrentUser.Gender) ? "Мужской" : CurrentUser.Gender;

            // Парсим дату рождения
            if (!string.IsNullOrEmpty(CurrentUser.BirthDate))
            {
                if (DateTime.TryParse(CurrentUser.BirthDate, out var parsedDate))
                {
                    BirthDate = parsedDate;
                }
            }

            Languages = CurrentUser.Languages;
            Country = CurrentUser.Country;
            City = CurrentUser.City;
            Bio = CurrentUser.Bio;
            SelectedStatus = CurrentUser.Status;

            // Загружаем аватар
            LoadAvatar();

            // Обновляем информацию об аватаре
            UpdateAvatarInfo();
        }



        // Команда для проверки доступности имени пользователя
        [RelayCommand]
        private async Task CheckUsernameAvailability()
        {
            if (CurrentUser == null || string.IsNullOrWhiteSpace(Username))
                return;

            try
            {
                IsCheckingAvailability = true;

                // Если имя пользователя не изменилось, оно доступно
                if (Username == OriginalUsername)
                {
                    IsUsernameAvailable = true;
                    return;
                }

                // Проверяем, занято ли имя пользователя
                var existingUser = await _firebaseService.GetUserByUsernameAsync(Username);

                IsUsernameAvailable = existingUser == null;

                if (!IsUsernameAvailable)
                {
                    ErrorMessage = "Имя пользователя уже занято";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CheckUsernameAvailability error: {ex.Message}");
                IsUsernameAvailable = false;
            }
            finally
            {
                IsCheckingAvailability = false;
            }
        }


        // Команда для проверки доступности email
        [RelayCommand]
        private async Task CheckEmailAvailability()
        {
            if (CurrentUser == null || string.IsNullOrWhiteSpace(Email))
                return;

            try
            {
                IsCheckingAvailability = true;

                // Если email не изменился, он доступен
                if (Email == OriginalEmail)
                {
                    IsEmailAvailable = true;
                    return;
                }

                // Проверяем, занят ли email
                var existingUser = await _firebaseService.GetUserByEmailAsync(Email);

                IsEmailAvailable = existingUser == null;

                if (!IsEmailAvailable)
                {
                    ErrorMessage = "Email уже используется другим пользователем";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CheckEmailAvailability error: {ex.Message}");
                IsEmailAvailable = false;
            }
            finally
            {
                IsCheckingAvailability = false;
            }
        }

        // Обновляем проверку при изменении полей
        partial void OnUsernameChanged(string value)
        {
            if (!string.IsNullOrEmpty(value) && value != OriginalUsername)
            {
                // Автоматически проверяем через 1 секунду после прекращения ввода
                DebounceCheckUsername();
            }
            else if (value == OriginalUsername)
            {
                IsUsernameAvailable = true;
            }
        }

        partial void OnEmailChanged(string value)
        {
            if (!string.IsNullOrEmpty(value) && value != OriginalEmail)
            {
                // Автоматически проверяем через 1 секунду после прекращения ввода
                DebounceCheckEmail();
            }
            else if (value == OriginalEmail)
            {
                IsEmailAvailable = true;
            }
        }

        // Дебаунс для предотвращения множественных запросов
        private async void DebounceCheckUsername()
        {
            await Task.Delay(1000); // Ждем 1 секунду
            if (Username == _username) // Проверяем, что значение не изменилось
            {
                await CheckUsernameAvailabilityCommand.ExecuteAsync(null);
            }
        }

        private async void DebounceCheckEmail()
        {
            await Task.Delay(1000);
            if (Email == _email)
            {
                await CheckEmailAvailabilityCommand.ExecuteAsync(null);
            }
        }

        private void UpdateAvatarInfo()
        {
            if (CurrentUser == null)
            {
                AvatarInfo = "Пользователь не загружен";
                AvatarStatus = "Неизвестно";
                AvatarStatusColor = new SolidColorBrush(Color.FromRgb(200, 200, 200));
                return;
            }

            // Определяем статус аватара
            if (HasAvatarChanges && !string.IsNullOrEmpty(SelectedAvatarPath))
            {
                // Есть несохраненные изменения
                AvatarInfo = "Выбран новый аватар (не сохранен)";
                AvatarStatus = "Не сохранено";
                AvatarStatusColor = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Желтый
            }
            else if (string.IsNullOrEmpty(CurrentUser.AvatarUrl))
            {
                // Нет аватара
                AvatarInfo = "Аватар не установлен";
                AvatarStatus = "По умолчанию";
                AvatarStatusColor = new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Серый
            }
            else if (CurrentUser.AvatarUrl.StartsWith("http"))
            {
                // Аватар в интернете (ImgBB)
                if (CurrentUser.AvatarUrl.Contains("imgbb"))
                {
                    AvatarInfo = "Аватар на ImgBB";
                    AvatarStatus = "Онлайн";
                    AvatarStatusColor = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Зеленый
                }
                else
                {
                    AvatarInfo = "Аватар в интернете";
                    AvatarStatus = "Онлайн";
                    AvatarStatusColor = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Зеленый
                }
            }
            else if (File.Exists(CurrentUser.AvatarUrl))
            {
                // Локальный аватар
                AvatarInfo = "Локальный аватар";
                AvatarStatus = "Локальный";
                AvatarStatusColor = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Оранжевый
            }
            else
            {
                // Аватар недоступен
                AvatarInfo = "Аватар недоступен";
                AvatarStatus = "Ошибка";
                AvatarStatusColor = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Красный
            }
        }

        private async void LoadAvatar()
        {
            try
            {
                // Сначала сбрасываем
                AvatarImage = null;
                IsAvatarDefault = true;

                if (!string.IsNullOrEmpty(CurrentUser!.AvatarUrl))
                {
                    if (CurrentUser.AvatarUrl.StartsWith("http"))
                    {
                        // Загружаем из интернета (ImgBB)
                        await LoadAvatarFromUrlAsync(CurrentUser.AvatarUrl);
                    }
                    else if (File.Exists(CurrentUser.AvatarUrl))
                    {
                        // Загружаем локальный файл
                        LoadAvatarFromFile(CurrentUser.AvatarUrl);
                    }
                    else
                    {
                        // Файл не найден, используем дефолтный аватар
                        SetDefaultAvatar();
                    }
                }
                else
                {
                    // Нет аватара, используем дефолтный
                    SetDefaultAvatar();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки аватара: {ex.Message}");
                SetDefaultAvatar();
            }
        }

        private async Task LoadAvatarFromUrlAsync(string url)
        {
            try
            {
                Console.WriteLine($" [LoadAvatarFromUrlAsync] Loading avatar from URL: {url}");

                // Сначала очищаем текущее изображение
                AvatarImage = null;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache; 
                bitmap.UriSource = new Uri(url + "?t=" + DateTime.Now.Ticks); // Добавляем timestamp для предотвращения кэширования
                bitmap.EndInit();

                // Ждём завершения загрузки
                if (bitmap.IsDownloading)
                {
                    bitmap.DownloadCompleted += (s, e) =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Console.WriteLine($"✅ Avatar downloaded successfully");
                            AvatarImage = bitmap;
                            IsAvatarDefault = false;
                            UpdateAvatarInfo();
                        });
                    };

                    bitmap.DownloadFailed += (s, e) =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Console.WriteLine($"❌ Avatar download failed");
                            SetDefaultAvatar();
                        });
                    };
                }
                else
                {
                    // Изображение уже загружено (например, из кэша)
                    AvatarImage = bitmap;
                    IsAvatarDefault = false;
                    UpdateAvatarInfo();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ LoadAvatarFromUrlAsync error: {ex.Message}");
                SetDefaultAvatar();
            }
        }



        private void LoadAvatarFromFile(string filePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                AvatarImage = bitmap;
                IsAvatarDefault = false; 
                SelectedAvatarPath = filePath;

                // Обновляем информацию
                UpdateAvatarInfo();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки аватара из файла: {ex.Message}");
                SetDefaultAvatar();
            }
        }

        private void SetDefaultAvatar()
        {
            try
            {
                // Создаем аватар с инициалами пользователя
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
                    var initials = GetUserInitials();
                    var text = new FormattedText(
                        initials,
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

                //  преобразуем RenderTargetBitmap в BitmapImage
                var renderBitmap = new RenderTargetBitmap(100, 100, 96, 96, PixelFormats.Pbgra32);
                renderBitmap.Render(drawingVisual);

                // Конвертируем в BitmapImage
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
                IsAvatarDefault = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания аватара: {ex.Message}");
                // Устанавливаем null или создаем простой BitmapImage
                AvatarImage = null;
                IsAvatarDefault = true;
            }
        }

        [RelayCommand]
        private void RemoveAvatar()
        {
            if (CurrentUser == null)
                return;

            var result = MessageBox.Show(
                "Удалить текущий аватар? Он будет заменен на стандартный.",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                CurrentUser.AvatarUrl = string.Empty;
                HasAvatarChanges = true;
                SetDefaultAvatar();
                UpdateAvatarInfo();

                MessageBox.Show("Аватар удален. Нажмите 'Сохранить' для применения изменений",
                    "Информация");
            }
        }

        // Метод для проверки валидности email
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private string GetUserInitials()
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

        [RelayCommand]
        private void StartEditing()
        {
            IsEditing = true;
            ErrorMessage = string.Empty;
        }

        [RelayCommand]
        private void CancelEditing()
        {
            IsEditing = false;
            LoadUserData(); 
            ErrorMessage = string.Empty;
        }

        [RelayCommand]
        private async Task SaveProfile()
        {
            if (CurrentUser == null)
                return;

            // Проверяем обязательные поля
            if (string.IsNullOrWhiteSpace(DisplayName))
            {
                ErrorMessage = "Имя пользователя не может быть пустым";
                return;
            }

            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "Никнейм не может быть пустым";
                return;
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "Email не может быть пустым";
                return;
            }

            // Проверяем валидность email
            if (!IsValidEmail(Email))
            {
                ErrorMessage = "Введите корректный email адрес";
                return;
            }

            // Проверяем доступность имени пользователя
            if (Username != OriginalUsername && !IsUsernameAvailable)
            {
                ErrorMessage = "Имя пользователя уже занято. Выберите другое.";
                return;
            }

            // Проверяем доступность email
            if (Email != OriginalEmail && !IsEmailAvailable)
            {
                ErrorMessage = "Email уже используется другим пользователем";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                Console.WriteLine($"[SaveProfile] Starting profile save...");

                // Сохраняем оригинальные значения
                var originalAvatarUrl = CurrentUser.AvatarUrl;

                // Обновляем данные пользователя
                CurrentUser.Username = Username.Trim();
                CurrentUser.DisplayName = DisplayName.Trim();
                CurrentUser.Email = Email.Trim();
                CurrentUser.Gender = SelectedGender;
                CurrentUser.BirthDate = BirthDate?.ToString("dd.MM.yyyy") ?? string.Empty;
                CurrentUser.Languages = Languages.Trim();
                CurrentUser.Country = Country.Trim();
                CurrentUser.City = City.Trim();
                CurrentUser.Bio = Bio.Trim();
                CurrentUser.Status = SelectedStatus;
                CurrentUser.LastSeen = DateTime.UtcNow;

                // Обновляем локальные оригинальные значения
                OriginalUsername = CurrentUser.Username;
                OriginalEmail = CurrentUser.Email;
                OriginalDisplayName = CurrentUser.DisplayName;

                // Если есть локальный аватар
                if (HasAvatarChanges && !string.IsNullOrEmpty(SelectedAvatarPath) &&
                    File.Exists(SelectedAvatarPath) &&
                    (string.IsNullOrEmpty(CurrentUser.AvatarUrl) || !CurrentUser.AvatarUrl.StartsWith("http")))
                {
                    Console.WriteLine($"[SaveProfile] Setting local avatar: {SelectedAvatarPath}");
                    CurrentUser.AvatarUrl = SelectedAvatarPath;
                }

                // Логируем изменения аватара
                if (originalAvatarUrl != CurrentUser.AvatarUrl)
                {
                    Console.WriteLine($"[SaveProfile] Avatar changed: '{originalAvatarUrl}' → '{CurrentUser.AvatarUrl}'");
                }

                // Сохраняем в Firebase
                Console.WriteLine($"[SaveProfile] Saving to Firebase...");
                var success = await _firebaseService.UpdateUserAsync(CurrentUser);

                if (!success)
                {
                    ErrorMessage = "Не удалось сохранить данные на сервере";
                    return;
                }

                // Сохраняем локально
                _localStorage.SaveUser(CurrentUser);

                // Обновляем статическое свойство
                User.CurrentUser = CurrentUser;

                // Перезагружаем аватар
                Console.WriteLine($"[SaveProfile] Reloading avatar...");
                LoadAvatar();

                // Сбрасываем изменения аватара
                HasAvatarChanges = false;
                SelectedAvatarPath = string.Empty;

                // Сбрасываем проверки доступности
                IsUsernameAvailable = true;
                IsEmailAvailable = true;

                // Обновляем информацию об аватаре
                UpdateAvatarInfo();

                // Сохраняем настройки темы
                await SaveUserSettings();

                IsEditing = false;

                MessageBox.Show("Профиль и настройки успешно сохранены!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                UpdateAvatarInMainWindow();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка сохранения: {ex.Message}";
                Console.WriteLine($"❌ SaveProfile error: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ChangeAvatar()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Изображения (*.jpg;*.jpeg;*.png;*.gif;*.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp|Все файлы (*.*)|*.*",
                    Title = "Выберите изображение для аватара",
                    Multiselect = false
                };

                if (dialog.ShowDialog() == true)
                {
                    var filePath = dialog.FileName;
                    var fileInfo = new FileInfo(filePath);

                    // Проверяем размер
                    if (fileInfo.Length > 5 * 1024 * 1024)
                    {
                        MessageBox.Show("Размер файла не должен превышать 5MB", "Ошибка");
                        return;
                    }

                    // Загружаем превью
                    LoadAvatarFromFile(filePath);
                    SelectedAvatarPath = filePath;

                    // ВАЖНО: Устанавливаем изменения
                    HasAvatarChanges = true;

                    // Обновляем информацию
                    UpdateAvatarInfo();

                    // Показываем кнопки действий
                    var result = MessageBox.Show(
                        "Аватар выбран. Что вы хотите сделать?\n\n" +
                        "✅ Сразу загрузить на ImgBB\n" +
                        "💾 Оставить локально (только на этом устройстве)",
                        "Аватар",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Сразу загружаем на ImgBB
                        await UploadAvatarToImgBB(filePath);
                    }
                    else
                    {
                        // Сохраняем локально
                        CurrentUser!.AvatarUrl = filePath;
                        UpdateAvatarInfo();
                        MessageBox.Show("Аватар сохранен локально. Нажмите 'Сохранить' для применения", "Информация");
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка выбора аватара: {ex.Message}";
                Console.WriteLine($"Change avatar error: {ex}");
            }
        }

        [RelayCommand]
        private async Task UploadAvatarToImgBB(string filePath = null)
        {
            if (CurrentUser == null)
                return;

            var path = filePath ?? SelectedAvatarPath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                MessageBox.Show("Сначала выберите файл для загрузки", "Ошибка");
                return;
            }

            try
            {
                IsUploadingAvatar = true;
                ErrorMessage = string.Empty;

                MessageBox.Show("Загружаю аватар на ImgBB...", "Информация");

                // Загружаем на ImgBB
                var imageUrl = await _imgBBService.UploadImageAsync(path);

                if (!string.IsNullOrEmpty(imageUrl))
                {
                    Console.WriteLine($"[ProfileVM] ImgBB upload successful: {imageUrl}");

                    // Обновляем URL аватара пользователя
                    CurrentUser.AvatarUrl = imageUrl;

                    // ОБНОВЛЯЕМ НЕМЕДЛЕННО в Firebase (отдельный вызов)
                    await _firebaseService.UpdateUserAvatarAsync(CurrentUser.Id, imageUrl);

                    // Обновляем локально
                    _localStorage.SaveUser(CurrentUser);
                    User.CurrentUser = CurrentUser;

                    // Загружаем аватар с нового URL
                    await LoadAvatarFromUrlAsync(imageUrl);

                    // Сбрасываем флаги
                    HasAvatarChanges = false;
                    SelectedAvatarPath = string.Empty;

                    // Обновляем информацию
                    UpdateAvatarInfo();
                    UpdateAvatarInMainWindow();


                    MessageBox.Show("Аватар успешно загружен на ImgBB и сохранен в профиле!",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    ErrorMessage = "Не удалось загрузить аватар на ImgBB";
                    MessageBox.Show("Не удалось загрузить аватар на ImgBB", "Ошибка");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки аватара: {ex.Message}";
                Console.WriteLine($"❌ UploadAvatarToImgBB error: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Ошибка загрузки аватара: {ex.Message}", "Ошибка");
            }
            finally
            {
                IsUploadingAvatar = false;
            }
        }

        [RelayCommand]
        private void BackToMain()
        {
            _navigationService.NavigateToMain();
        }

        [RelayCommand]
        private async Task ChangePassword()
        {
            var result = MessageBox.Show("Отправить ссылку для сброса пароля на вашу почту?",
                "Смена пароля", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes && CurrentUser != null)
            {
                try
                {
                    MessageBox.Show("Ссылка для сброса пароля отправлена на вашу почту",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}