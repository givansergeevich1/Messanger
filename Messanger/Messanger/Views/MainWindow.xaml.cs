using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using System.Windows.Media;
using Messenger.Models;
using Messenger.Services;
using Messenger.ViewModels;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Controls;
using Messenger.Themes;

namespace Messenger.Views
{
    public partial class MainWindow : Window
    {

        private Popup _notificationPopup;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                this.DataContext = new MainViewModel();
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ Ошибка в конструкторе: {ex.Message}", "Critical Error");
                throw;
            }
        }


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        private const uint FLASHW_ALL = 0x00000003;
        private const uint FLASHW_TIMERNOFG = 0x0000000C;

        public void FlashWindow()
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                var fwi = new FLASHWINFO
                {
                    cbSize = Convert.ToUInt32(Marshal.SizeOf(typeof(FLASHWINFO))),
                    hwnd = hwnd,
                    dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG,
                    uCount = uint.MaxValue,
                    dwTimeout = 0
                };

                FlashWindowEx(ref fwi);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка мигания окна: {ex.Message}");
            }
        }

        // Основной метод уведомления (Popup)
        public void ShowNotification(string title, string message)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    // Закрываем предыдущее уведомление
                    if (_notificationPopup != null && _notificationPopup.IsOpen)
                    {
                        _notificationPopup.IsOpen = false;
                    }

                    // Создаём новое уведомление
                    _notificationPopup = new Popup
                    {
                        Width = 300,
                        Height = 80,
                        AllowsTransparency = true,
                        Placement = PlacementMode.Absolute,
                        PlacementRectangle = new Rect(
                            SystemParameters.WorkArea.Right - 320,
                            SystemParameters.WorkArea.Bottom - 100,
                            300, 80),
                        IsOpen = true
                    };

                    // Создаём содержимое уведомления
                    var border = new System.Windows.Controls.Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)),
                        BorderBrush = new SolidColorBrush(Color.FromArgb(200, 200, 220, 240)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(8),
                        Effect = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            BlurRadius = 10,
                            Color = Colors.Black,
                            Opacity = 0.3,
                            ShadowDepth = 2
                        }
                    };

                    var stackPanel = new System.Windows.Controls.StackPanel
                    {
                        Margin = new Thickness(15)
                    };

                    var titleText = new System.Windows.Controls.TextBlock
                    {
                        Text = title,
                        FontWeight = FontWeights.Bold,
                        FontSize = 13,
                        Foreground = new SolidColorBrush(Color.FromRgb(30, 60, 120))
                    };

                    var messageText = new System.Windows.Controls.TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 12,
                        Margin = new Thickness(0, 5, 0, 0),
                        Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 100))
                    };

                    stackPanel.Children.Add(titleText);
                    stackPanel.Children.Add(messageText);
                    border.Child = stackPanel;
                    _notificationPopup.Child = border;

                    // Анимация появления
                    border.Opacity = 0;
                    var fadeIn = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(0.3)
                    };
                    border.BeginAnimation(UIElement.OpacityProperty, fadeIn);

                    // Автоматически закрываем через 3 секунды
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(3)
                    };

                    timer.Tick += (sender, args) =>
                    {
                        timer.Stop();

                        // Анимация исчезновения
                        var fadeOut = new DoubleAnimation
                        {
                            From = 1,
                            To = 0,
                            Duration = TimeSpan.FromSeconds(0.3)
                        };

                        fadeOut.Completed += (s, e) =>
                        {
                            if (_notificationPopup != null)
                            {
                                _notificationPopup.IsOpen = false;
                                _notificationPopup = null;
                            }
                        };

                        border.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                    };

                    timer.Start();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка показа уведомления: {ex.Message}");
                // Если не сработало, показываем простой MessageBox
                Dispatcher.Invoke(() => MessageBox.Show(message, title));
            }
        }

        // Метод для остановки мигания (при активации окна)
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            StopFlashing();
        }

        private void StopFlashing()
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                var fwi = new FLASHWINFO
                {
                    cbSize = Convert.ToUInt32(Marshal.SizeOf(typeof(FLASHWINFO))),
                    hwnd = hwnd,
                    dwFlags = 0,
                    uCount = 0,
                    dwTimeout = 0
                };

                FlashWindowEx(ref fwi);
            }
            catch { }
        }

        // Простой метод уведомления (для использования из ViewModel)
        public void ShowSimpleNotification(string title, string message)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    // Изменяем заголовок окна на 3 секунды
                    var originalTitle = this.Title;
                    this.Title = $"🔔 {title} - {message}";

                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(3)
                    };

                    timer.Tick += (s, e) =>
                    {
                        timer.Stop();
                        this.Title = originalTitle;
                    };

                    timer.Start();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка простого уведомления: {ex.Message}");
            }
        }

        private void ShowMessageNotification(string sender, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Используем новый метод ShowNotification
                ShowNotification("Новое сообщение", $"{sender}: {message}");
            });
        }


        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {

                // Проверяем есть ли текущий пользователь
                if (User.CurrentUser == null)
                {
                    // Пробуем загрузить из локального хранилища
                    var localStorage = new LocalStorageService();
                    var localUser = localStorage.GetCurrentUser();

                    if (localUser != null)
                    {
                        User.CurrentUser = localUser;
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("❌ НЕТ ПОЛЬЗОВАТЕЛЯ! Возвращаемся к входу.", "Ошибка");

                        var loginWindow = new LoginWindow();
                        loginWindow.Show();

                        this.Close();
                        return;
                    }
                }

               if (DataContext is MainViewModel viewModel)
                {
                    viewModel.CurrentUser = User.CurrentUser;
                }

               MessageInput.Focus();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ Ошибка загрузки MainWindow: {ex.Message}\n{ex.StackTrace}", "Critical Error");

                // При ошибке возвращаемся к входу
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            // Закрываем уведомление если открыто
            if (_notificationPopup != null && _notificationPopup.IsOpen)
            {
                _notificationPopup.IsOpen = false;
            }
        }

        private void MessageInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            
            if (e.Key == System.Windows.Input.Key.Enter &&
                !e.KeyboardDevice.IsKeyDown(System.Windows.Input.Key.LeftShift) &&
                !e.KeyboardDevice.IsKeyDown(System.Windows.Input.Key.RightShift))
            {
                if (DataContext is MainViewModel mainVM)
                {
                    mainVM.SendMessageCommand.Execute(null);
                    e.Handled = true; 
                }
            }
        }

        private void ChatList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                // Проверяем, инициализировано ли поле ввода
                if (MessageInput != null && MessageInput.IsLoaded)
                {
                    // Используем Dispatcher для безопасного доступа к UI
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            MessageInput.Focus();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Focus error: {ex.Message}");
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ChatList_SelectionChanged error: {ex.Message}");
            }
        }

        private void TestNotification_Click(object sender, RoutedEventArgs e)
        {
            ShowNotification("Тест", "Это тестовое уведомление!");
        }
        private void CopyMessageText_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is MenuItem menuItem && menuItem.DataContext is Message message)
                {
                    if (!string.IsNullOrEmpty(message.Content))
                    {
                        Clipboard.SetText(message.Content);
                        MessageBox.Show("Текст скопирован в буфер обмена", "Успех");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка копирования: {ex.Message}", "Ошибка");
            }
        }

        private async void EditMessage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is MenuItem menuItem && menuItem.DataContext is Message message)
                {
                    if (DataContext is MainViewModel vm)
                    {
                        await vm.EditMessageCommand.ExecuteAsync(message);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка редактирования: {ex.Message}", "Ошибка");
            }
        }

        private async void DeleteMessage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is MenuItem menuItem && menuItem.DataContext is Message message)
                {
                    // Подтверждение
                    var result = MessageBox.Show(
                        $"Удалить сообщение?\n\n\"{message.Content}\"",
                        "Подтверждение удаления",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        if (DataContext is MainViewModel vm)
                        {
                            await vm.DeleteMessageCommand.ExecuteAsync(message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка");
            }
        }

        private async void DownloadFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is MenuItem menuItem && menuItem.DataContext is Message message)
                {
                    if (DataContext is MainViewModel vm)
                    {
                        await vm.DownloadFileCommand.ExecuteAsync(message);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка скачивания: {ex.Message}", "Ошибка");
            }
        }

        public void UpdateThemeColors()
        {
            // Обновляем цвет фона окна
            this.Background = ThemeManager.IsDarkTheme ?
                new SolidColorBrush(Color.FromRgb(30, 30, 35)) :
                new SolidColorBrush(Color.FromRgb(245, 245, 245));

            // Левая панель
            if (LeftPanelGrid != null)
            {
                LeftPanelGrid.Background = ThemeManager.IsDarkTheme ?
                    new SolidColorBrush(Color.FromRgb(40, 40, 45)) :
                    new SolidColorBrush(Color.FromRgb(248, 249, 250));
            }

            // Поле ввода сообщений
            if (MessageInput != null)
            {
                MessageInput.Foreground = ThemeManager.IsDarkTheme ? Brushes.White : Brushes.Black;
                MessageInput.Background = ThemeManager.IsDarkTheme ?
                    new SolidColorBrush(Color.FromRgb(60, 60, 65)) : Brushes.White;
            }

            // Поле поиска
            if (SearchTextBox != null)
            {
                SearchTextBox.Foreground = ThemeManager.IsDarkTheme ? Brushes.White : Brushes.Black;
                SearchTextBox.Background = ThemeManager.IsDarkTheme ?
                    new SolidColorBrush(Color.FromRgb(60, 60, 65)) : Brushes.White;
            }
        }

        private void ShowMessageInfo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is MenuItem menuItem && menuItem.DataContext is Message message)
                {
                    if (DataContext is MainViewModel vm)
                    {
                        vm.ShowMessageInfoCommand.Execute(message);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }
    }
}