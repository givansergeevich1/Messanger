using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Database;
using Firebase.Database.Streaming;
using Messenger.Models;
using Messenger.Services;
using Messenger.Themes;
using Messenger.Views;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Messenger.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly FirebaseService _firebaseService;
        private readonly ChatService _chatService;
        private readonly LocalStorageService _localStorage;
        private readonly FileService _fileService;
        private readonly DownloadLogger _downloadLogger;

        [ObservableProperty]
        private User? _currentUser;

        [ObservableProperty]
        private ObservableCollection<Chat> _chats = new();

        [ObservableProperty]
        private Chat? _selectedChat;

        [ObservableProperty]
        private ObservableCollection<Message> _messages = new();

        [ObservableProperty]
        private string _newMessage = string.Empty;

        [ObservableProperty]
        private string _searchText = string.Empty; // Для поиска в списке чатов

        [ObservableProperty]
        private ObservableCollection<Chat> _filteredChats = new();

        [ObservableProperty]
        private BitmapImage? _userAvatarImage;

        [ObservableProperty]
        private bool _hasUserAvatar = false;

        [ObservableProperty]
        private string _userInitials = "U";

        private IDisposable? _messagesSubscription;

        public MainViewModel()
        {
            try
            {
                CleanTempFiles();
                // Создаем сервисы
                _firebaseService = new FirebaseService();
                _localStorage = new LocalStorageService();
                _chatService = new ChatService(_firebaseService, _localStorage);
                _fileService = new FileService();
                _downloadLogger = new DownloadLogger();

                // Подписываемся на изменение аватара
                User.AvatarChanged += OnUserAvatarChanged;

                // Загружаем текущего пользователя
                LoadCurrentUser();

                // Инициализируем чаты
                InitializeChats();

                // Инициализируем отфильтрованные чаты
                FilteredChats = new ObservableCollection<Chat>(Chats);

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ Ошибка в MainViewModel: {ex.Message}", "Critical Error");
                throw;
            }

        }

        private async void OnUserAvatarChanged(object sender, EventArgs e)
        {
            Console.WriteLine("Avatar changed event received");

            if (CurrentUser != null && sender is User user)
            {
                // Если это изменение текущего пользователя
                if (user.Id == CurrentUser.Id)
                {
                    // Перезагружаем аватар
                    await CurrentUser.LoadAvatarAsync();

                    // Уведомляем UI об изменении
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        OnPropertyChanged(nameof(CurrentUser));
                    });
                }
            }
        }

        // Команды для работы с сообщениями
        [RelayCommand]
        private async Task EditMessage(Message message)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== EDIT MESSAGE COMMAND ===");
                System.Diagnostics.Debug.WriteLine($"Message ID: {message?.Id}");
                System.Diagnostics.Debug.WriteLine($"IsDeleted: {message?.IsDeleted}");
                System.Diagnostics.Debug.WriteLine($"CurrentUser: {CurrentUser?.Id}");
                System.Diagnostics.Debug.WriteLine($"Message SenderId: {message?.SenderId}");
                System.Diagnostics.Debug.WriteLine($"Can edit: {CurrentUser?.Id == message?.SenderId}");

                if (message == null || message.IsDeleted || CurrentUser == null || message.SenderId != CurrentUser.Id)
                {
                    System.Diagnostics.Debug.WriteLine("Validation failed - cannot edit");
                    return;
                }

                // Простой диалог для теста
                var newContent = Microsoft.VisualBasic.Interaction.InputBox(
                    "Редактировать сообщение:",
                    "Редактирование",
                    message.Content);

                if (string.IsNullOrWhiteSpace(newContent) || newContent == message.Content)
                {
                    System.Diagnostics.Debug.WriteLine("Content unchanged or empty");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Old content: '{message.Content}'");
                System.Diagnostics.Debug.WriteLine($"New content: '{newContent}'");

                // Сохраняем старый контент на случай отката
                var oldContent = message.Content;

                // 1. Обновляем объект сообщения
                message.Content = newContent;
                message.IsEdited = true;
                message.EditedAt = DateTime.UtcNow;
                message.EditedBy = CurrentUser.Id;

                System.Diagnostics.Debug.WriteLine($"Message object updated");
                System.Diagnostics.Debug.WriteLine($"IsEdited: {message.IsEdited}");
                System.Diagnostics.Debug.WriteLine($"EditedAt: {message.EditedAt}");

                // 2. Обновляем в коллекции (это важно!)
                var index = Messages.IndexOf(message);
                System.Diagnostics.Debug.WriteLine($"Index in Messages: {index}");

                if (index >= 0)
                {
                    // Ключевой момент: создаем НОВЫЙ объект для обновления коллекции
                    var updatedMessage = new Message
                    {
                        Id = message.Id,
                        ChatId = message.ChatId,
                        SenderId = message.SenderId,
                        SenderName = message.SenderName,
                        Content = message.Content,
                        IsEdited = message.IsEdited,
                        EditedAt = message.EditedAt,
                        EditedBy = message.EditedBy,
                        CreatedAt = message.CreatedAt,
                        Status = message.Status,
                        MessageType = message.MessageType,
                        FileAttachment = message.FileAttachment,
                        IsDeleted = message.IsDeleted
                    };

                    Messages[index] = updatedMessage;
                    System.Diagnostics.Debug.WriteLine($"Message replaced in collection at index {index}");

                    // Принудительно обновляем UI
                    OnPropertyChanged(nameof(Messages));
                }

                // 3. Пробуем отправить в Firebase
                try
                {
                    System.Diagnostics.Debug.WriteLine("Trying to update in Firebase...");
                    var success = await _firebaseService.UpdateMessageAsync(message);
                    System.Diagnostics.Debug.WriteLine($"Firebase update result: {success}");

                    MessageBox.Show("Сообщение отредактировано", "Успех");
                }
                catch (FirebaseException firebaseEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Firebase error: {firebaseEx.Message}");

                    // Откатываем изменения
                    message.Content = oldContent;
                    message.IsEdited = false;
                    message.EditedAt = null;
                    message.EditedBy = null;

                    if (index >= 0)
                    {
                        Messages[index] = message;
                        OnPropertyChanged(nameof(Messages));
                    }

                    MessageBox.Show($"Ошибка Firebase: {firebaseEx.Message}", "Ошибка");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EDIT ERROR: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }

        [RelayCommand]
        private void ShowMessageInfo(Message message)
        {
            if (message == null) return;

            var info = $"📝 Информация о сообщении:\n\n" +
                       $"ID: {message.Id}\n" +
                       $"Отправитель: {message.SenderName}\n" +
                       $"Время отправки: {message.CreatedAt.ToLocalTime():dd.MM.yyyy HH:mm}\n";

            if (message.IsEdited && message.EditedAt.HasValue)
            {
                info += $"✏️ Отредактировано: {message.EditedAt.Value.ToLocalTime():dd.MM.yyyy HH:mm}\n";
                if (!string.IsNullOrEmpty(message.EditedBy))
                {
                    info += $"Кем: {message.EditedBy}\n";
                }
            }

            if (message.IsDeleted && message.DeletedAt.HasValue)
            {
                info += $"🗑️ Удалено: {message.DeletedAt.Value.ToLocalTime():dd.MM.yyyy HH:mm}\n";
                if (!string.IsNullOrEmpty(message.DeletedBy))
                {
                    info += $"Кем: {message.DeletedBy}\n";
                }
            }

            if (message.HasAttachment)
            {
                info += $"\n📎 Прикрепленный файл: {message.FileAttachment.FileName}\n";
                info += $"Размер: {message.FileAttachment.FileSizeFormatted}\n";
            }

            MessageBox.Show(info, "Информация о сообщении");
        }

        [RelayCommand]
        private async Task DeleteMessage(Message message)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== DELETE MESSAGE COMMAND ===");

                if (message == null || message.IsDeleted || CurrentUser == null)
                    return;

                bool canDelete = message.SenderId == CurrentUser.Id || CurrentUser.IsAdmin;
                if (!canDelete)
                {
                    MessageBox.Show("Вы можете удалять только свои сообщения", "Ошибка");
                    return;
                }

                var result = MessageBox.Show(
                    $"Удалить сообщение?\n\n\"{message.Content}\"",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // 1. Создаём копию с флагом удаления
                    var deletedMessage = new Message
                    {
                        Id = message.Id,
                        ChatId = message.ChatId,
                        SenderId = message.SenderId,
                        SenderName = message.SenderName,
                        Content = "Сообщение удалено",
                        IsDeleted = true,
                        DeletedAt = DateTime.UtcNow,
                        DeletedBy = CurrentUser.Id,
                        CreatedAt = message.CreatedAt,
                        IsEdited = message.IsEdited,
                        EditedAt = message.EditedAt,
                        EditedBy = message.EditedBy,
                        Status = message.Status,
                        MessageType = message.MessageType,
                        FileAttachment = message.FileAttachment
                    };

                    // 2. Немедленно обновляем в UI
                    var index = Messages.IndexOf(message);
                    if (index >= 0)
                    {
                        Messages[index] = deletedMessage;
                        System.Diagnostics.Debug.WriteLine($"Message updated in UI immediately");
                    }

                    // 3. Отправляем в Firebase
                    try
                    {
                        await _firebaseService.UpdateMessageAsync(deletedMessage);
                        MessageBox.Show("Сообщение удалено", "Успех");
                    }
                    catch (FirebaseException firebaseEx)
                    {
                        // Откатываем в случае ошибки
                        if (index >= 0) Messages[index] = message;
                        MessageBox.Show($"Не удалось удалить из Firebase: {firebaseEx.Message}", "Ошибка");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка");
            }
        }

        [RelayCommand]
        private void ReplyToMessage(Message message)
        {
            if (message == null || message.IsDeleted)
                return;

            // Устанавливаем текст для ответа
            var replyText = $"> {message.SenderName}: {message.Content}\n";
            NewMessage = replyText + NewMessage;

            // Прокручиваем к полю ввода
            ScrollToMessageInput();
        }

        [RelayCommand]
        private void CopyMessageText(Message message)
        {
            if (message == null || message.IsDeleted)
                return;

            try
            {
                Clipboard.SetText(message.Content);
                MessageBox.Show("Текст скопирован в буфер обмена", "Успех");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка копирования: {ex.Message}", "Ошибка");
            }
        }

        private void ScrollToMessageInput()
        {
            try
            {
                var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                var messageTextBox = mainWindow?.FindName("MessageTextBox") as TextBox;
                messageTextBox?.Focus();
            }
            catch { }
        }




        // Команда для прикрепления файла
        [RelayCommand]
        private async Task AttachFile()
        {
            try
            {
                var fileInfo = _fileService.SelectFile();
                if (fileInfo == null)
                    return;

                var (filePath, fileName, fileSize, fileType) = fileInfo.Value;

                // Упрощённое подтверждение - без детальной информации
                var confirmResult = MessageBox.Show(
                    $"Отправить файл: {fileName}?",
                    "Подтверждение отправки",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmResult != MessageBoxResult.Yes)
                    return;

                // Создаём временную копию файла
                var tempFilePath = _fileService.CopyToTemp(filePath);

                // Отправляем файл
                await SendFileMessageAsync(tempFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке файла: {ex.Message}", "Ошибка отправки");
                // Логирование для отладки
                System.Diagnostics.Debug.WriteLine($"AttachFile error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // Метод отправки файлового сообщения
        private async Task SendFileMessageAsync(string filePath)
        {
            if (SelectedChat == null || CurrentUser == null)
            {
                MessageBox.Show("❌ Чат не выбран или пользователь не авторизован", "Ошибка");
                return;
            }

            try
            {
                // 1. Создаём сообщение
                var message = new Message(filePath)
                {
                    ChatId = SelectedChat.Id,
                    SenderId = CurrentUser.Id,
                    SenderName = CurrentUser.DisplayName,
                    Status = MessageStatus.Sending
                };

                // 2. Добавляем в UI
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Messages.Add(message);
                    ScrollToLastMessage();
                });

                // 3. ОПРЕДЕЛЯЕМ ТИП ФАЙЛА И ВЫБИРАЕМ СПОСОБ ОТПРАВКИ
                string fileUrl;

                if (IsImageFile(filePath))
                {
                    // Для изображений - загружаем на ImgBB
                    var imgbbService = new ImgBBService();
                    fileUrl = await imgbbService.UploadImageAsync(filePath);

                    if (string.IsNullOrEmpty(fileUrl))
                    {
                        throw new Exception("Не удалось загрузить изображение на ImgBB");
                    }
                }
                else
                {
                    // Для других файлов - локальное хранение
                    var sharingService = new LocalFileSharingService();
                    fileUrl = await sharingService.ShareFileLocally(
                        filePath,
                        SelectedChat.Id,
                        message.FileAttachment.FileName);

                    if (string.IsNullOrEmpty(fileUrl))
                    {
                        throw new Exception("Не удалось сохранить файл локально");
                    }
                }

                // 4. Обновляем сообщение с URL
                message.FileAttachment.Url = fileUrl;
                message.FileAttachment.IsUploaded = true;
                message.Status = MessageStatus.Sent;

                // 5. Обновляем в UI
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var index = Messages.IndexOf(message);
                    if (index >= 0)
                    {
                        Messages[index] = message;
                    }
                });

                // 6. Отправляем сообщение в Firebase
                await _firebaseService.SendMessageAsync(message);

                // 7. Обновляем последнее сообщение в чате
                SelectedChat.LastMessage = message;
                OnPropertyChanged(nameof(SelectedChat));

                MessageBox.Show($"✅ Файл отправлен: {Path.GetFileName(filePath)}", "Успех");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка отправки файла: {ex.Message}", "Ошибка");

                // Помечаем сообщение как неудачное
                var failedMessage = Messages.LastOrDefault(m => m.Status == MessageStatus.Sending);
                if (failedMessage != null)
                {
                    failedMessage.Status = MessageStatus.Failed;
                }
            }
        }

        // Вспомогательный метод для определения типа файла
        private bool IsImageFile(string filePath)
        {
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            var extension = Path.GetExtension(filePath)?.ToLower();
            return imageExtensions.Contains(extension);
        }

        // Команда для скачивания файла
        [RelayCommand]
        private async Task DownloadFile(Message message)
        {
            if (message?.FileAttachment == null || string.IsNullOrEmpty(message.FileAttachment.Url))
                return;

            string savePath = string.Empty;

            try
            {
                System.Diagnostics.Debug.WriteLine($"=== DOWNLOAD FILE ===");
                System.Diagnostics.Debug.WriteLine($"File: {message.FileAttachment.FileName}");
                System.Diagnostics.Debug.WriteLine($"URL: {message.FileAttachment.Url}");

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Сохранить файл",
                    FileName = message.FileAttachment.FileName,
                    Filter = "Все файлы (*.*)|*.*"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    savePath = saveFileDialog.FileName;

                    // Проверка места на диске
                    try
                    {
                        var drive = Path.GetPathRoot(savePath);
                        var driveInfo = new DriveInfo(drive);
                        if (driveInfo.AvailableFreeSpace < message.FileSize)
                        {
                            MessageBox.Show(
                                $"Недостаточно места на диске {drive}.\n" +
                                $"Требуется: {FormatBytes(message.FileSize)}\n" +
                                $"Доступно: {FormatBytes(driveInfo.AvailableFreeSpace)}",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                            return;
                        }
                    }
                    catch (Exception driveEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Drive check error: {driveEx.Message}");
                    }

                    if (message.FileAttachment.Url.StartsWith("messenger-file://"))
                    {
                        // Локальный файл
                        var sharingService = new LocalFileSharingService();
                        var localPath = sharingService.GetLocalFilePath(message.FileAttachment.Url);

                        if (!string.IsNullOrEmpty(localPath) && File.Exists(localPath))
                        {
                            File.Copy(localPath, savePath, true);
                            ShowDownloadSuccess(savePath, message.FileAttachment.FileSizeFormatted);

                            _downloadLogger.LogDownload(
                                message.FileAttachment.FileName,
                                "local-file",
                                savePath,
                                true);
                        }
                        else
                        {
                            MessageBox.Show("❌ Файл не найден локально", "Ошибка");

                            _downloadLogger.LogDownload(
                                message.FileAttachment.FileName,
                                message.FileAttachment.Url,
                                savePath,
                                false,
                                "Локальный файл не найден");
                        }
                    }
                    else
                    {
                        // Файл с ImgBB или другого URL
                        await DownloadFileSimple(message.FileAttachment.Url, savePath);

                        ShowDownloadSuccess(savePath, message.FileAttachment.FileSizeFormatted);

                        _downloadLogger.LogDownload(
                            message.FileAttachment.FileName,
                            message.FileAttachment.Url,
                            savePath,
                            true);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DOWNLOAD ERROR: {ex.Message}");

                if (!string.IsNullOrEmpty(savePath) && File.Exists(savePath))
                {
                    try { File.Delete(savePath); } catch { }
                }

                _downloadLogger.LogDownload(
                    message?.FileAttachment?.FileName ?? "Unknown",
                    message?.FileAttachment?.Url ?? "Unknown",
                    savePath,
                    false,
                    ex.Message);

                MessageBox.Show($"❌ Ошибка скачивания файла: {ex.Message}", "Ошибка");
            }
        }

        [RelayCommand]
        private void UnlockFiles()
        {
            try
            {
                // Принудительная очистка ресурсов
                GC.Collect();
                GC.WaitForPendingFinalizers();

                MessageBox.Show("Ресурсы освобождены. Теперь можно удалять файлы.", "Информация");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }

        private async Task DownloadFileSimple(string url, string savePath)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"DownloadFileSimple: {url}");

                // Создаем директорию если нужно
                var directory = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Простейший способ - WebClient
                using (var webClient = new System.Net.WebClient())
                {
                    await webClient.DownloadFileTaskAsync(new Uri(url), savePath);
                }

                System.Diagnostics.Debug.WriteLine($"DownloadFileSimple: успешно сохранено в {savePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DownloadFileSimple ERROR: {ex.Message}");

                // Удаляем частичный файл при ошибке
                if (File.Exists(savePath))
                {
                    try { File.Delete(savePath); } catch { }
                }

                throw;
            }
        }

        // Метод для форматирования байтов
        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        [RelayCommand]
        private void ViewDownloadLogs()
        {
            try
            {
                var logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Messenger",
                    "downloads.log");

                if (File.Exists(logPath))
                {
                    var logContent = File.ReadAllText(logPath);

                    if (string.IsNullOrWhiteSpace(logContent))
                    {
                        MessageBox.Show("Лог скачиваний пуст", "Информация");
                        return;
                    }

                    // Создаем простое окно для просмотра логов
                    var logWindow = new Window
                    {
                        Title = "Лог скачиваний файлов",
                        Width = 800,
                        Height = 600,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = Application.Current.MainWindow
                    };

                    var textBox = new TextBox
                    {
                        Text = logContent,
                        IsReadOnly = true,
                        FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                        FontSize = 12,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        TextWrapping = TextWrapping.NoWrap
                    };

                    var stackPanel = new StackPanel();

                    // Кнопки управления
                    var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };

                    var clearButton = new Button
                    {
                        Content = "Очистить логи",
                        Margin = new Thickness(5),
                        Padding = new Thickness(10, 5, 10, 5)
                    };

                    var openFolderButton = new Button
                    {
                        Content = "Открыть папку логов",
                        Margin = new Thickness(5),
                        Padding = new Thickness(10, 5, 10, 5)
                    };

                    var closeButton = new Button
                    {
                        Content = "Закрыть",
                        Margin = new Thickness(5),
                        Padding = new Thickness(10, 5, 10, 5)
                    };

                    clearButton.Click += (s, e) =>
                    {
                        var result = MessageBox.Show("Очистить все логи скачиваний?", "Подтверждение",
                            MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            File.WriteAllText(logPath, string.Empty);
                            textBox.Text = "Логи очищены.";
                        }
                    };

                    openFolderButton.Click += (s, e) =>
                    {
                        var folder = Path.GetDirectoryName(logPath);
                        if (Directory.Exists(folder))
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = folder,
                                UseShellExecute = true
                            });
                        }
                    };

                    closeButton.Click += (s, e) => logWindow.Close();

                    buttonPanel.Children.Add(clearButton);
                    buttonPanel.Children.Add(openFolderButton);
                    buttonPanel.Children.Add(closeButton);

                    stackPanel.Children.Add(buttonPanel);
                    stackPanel.Children.Add(textBox);

                    logWindow.Content = stackPanel;
                    logWindow.ShowDialog();
                }
                else
                {
                    MessageBox.Show("Файл логов не найден. Скачайте хотя бы один файл, чтобы создать лог.", "Информация");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии логов: {ex.Message}", "Ошибка");
            }
        }

        [RelayCommand]
        private void CleanupOldLogs()
        {
            try
            {
                var logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Messenger");

                if (Directory.Exists(logDir))
                {
                    int deletedCount = 0;
                    foreach (var file in Directory.GetFiles(logDir, "*.log"))
                    {
                        try
                        {
                            var fileInfo = new FileInfo(file);
                            // Удаляем логи старше 30 дней
                            if (fileInfo.LastWriteTime < DateTime.Now.AddDays(-30))
                            {
                                File.Delete(file);
                                deletedCount++;
                            }
                        }
                        catch { }
                    }

                    if (deletedCount > 0)
                    {
                        MessageBox.Show($"Удалено {deletedCount} старых файлов логов", "Информация");
                    }
                    else
                    {
                        MessageBox.Show("Старые логи не найдены", "Информация");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка очистки логов: {ex.Message}", "Ошибка");
            }
        }



        // Вспомогательный метод для показа успеха
        private void ShowDownloadSuccess(string filePath, string fileSize = "")
        {
            var fileName = Path.GetFileName(filePath);
            var message = $"✅ Файл сохранен:\n{fileName}\nПуть: {filePath}";

            if (!string.IsNullOrEmpty(fileSize))
            {
                message = $"✅ Файл сохранен ({fileSize}):\n{fileName}\nПуть: {filePath}";
            }

            var result = MessageBox.Show(
                message + "\n\nЧто вы хотите сделать?",
                "Скачивание завершено",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question,
                MessageBoxResult.Yes);

            if (result == MessageBoxResult.Yes)
            {
                // Открыть файл
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось открыть файл: {ex.Message}", "Ошибка");
                }
            }
            else if (result == MessageBoxResult.No)
            {
                // Открыть папку с файлом
                try
                {
                    var folderPath = Path.GetDirectoryName(filePath);
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = folderPath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось открыть папку: {ex.Message}", "Ошибка");
                }
            }
        }

        // Команда для открытия файла
        [RelayCommand]
        private void OpenFile(Message message)
        {
            if (message?.FileAttachment == null)
                return;

            try
            {
                // ВСЕГДА открываем через временную копию
                string sourceFilePath = null;

                if (!string.IsNullOrEmpty(message.FileAttachment.LocalPath) &&
                    File.Exists(message.FileAttachment.LocalPath))
                {
                    sourceFilePath = message.FileAttachment.LocalPath;
                }
                else if (!string.IsNullOrEmpty(message.FileAttachment.Url) &&
                         message.FileAttachment.Url.StartsWith("messenger-file://"))
                {
                    var sharingService = new LocalFileSharingService();
                    sourceFilePath = sharingService.GetLocalFilePath(message.FileAttachment.Url);
                }

                if (!string.IsNullOrEmpty(sourceFilePath) && File.Exists(sourceFilePath))
                {
                    // Создаем временную копию
                    var tempDir = Path.Combine(Path.GetTempPath(), "MessengerTemp");
                    Directory.CreateDirectory(tempDir);

                    var tempFilePath = Path.Combine(tempDir,
                        $"{Guid.NewGuid()}{Path.GetExtension(sourceFilePath)}");

                    File.Copy(sourceFilePath, tempFilePath, true);

                    // Открываем копию
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = tempFilePath,
                        UseShellExecute = true
                    });

                    // Удаляем через 10 секунд
                    Task.Delay(10000).ContinueWith(_ =>
                    {
                        try
                        {
                            if (File.Exists(tempFilePath))
                                File.Delete(tempFilePath);
                        }
                        catch { }
                    });
                }
                else if (!string.IsNullOrEmpty(message.FileAttachment.Url))
                {
                    // Открываем URL в браузере
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = message.FileAttachment.Url,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("Файл недоступен", "Информация");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Не удалось открыть файл: {ex.Message}", "Ошибка");
            }
        }

        private void CleanTempFiles()
        {
            try
            {
                var tempDir = Path.Combine(Path.GetTempPath(), "MessengerTemp");
                if (Directory.Exists(tempDir))
                {
                    foreach (var file in Directory.GetFiles(tempDir))
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        // Метод для отображения информации о файле
        [RelayCommand]
        private void ShowFileInfo(Message message)
        {
            if (message?.FileAttachment == null)
                return;

            var info = $"📄 Информация о файле:\n\n" +
                       $"Имя: {message.FileAttachment.FileName}\n" +
                       $"Размер: {message.FileAttachment.FileSizeFormatted}\n" +
                       $"Тип: {message.FileAttachment.FileType}\n" +
                       $"Отправлен: {message.CreatedAt.ToLocalTime():dd.MM.yyyy HH:mm}\n" +
                       $"Статус: {message.Status}\n";

            if (!string.IsNullOrEmpty(message.FileAttachment.Url))
                info += $"\nURL: {message.FileAttachment.Url}";

            if (!string.IsNullOrEmpty(message.FileAttachment.LocalPath))
                info += $"\nЛокальный путь: {message.FileAttachment.LocalPath}";

            MessageBox.Show(info, "Информация о файле");
        }

        private async void LoadCurrentUser()
        {
            try
            {
                if (User.CurrentUser == null)
                {
                    // Пробуем загрузить из LocalStorage
                    var localUser = _localStorage.GetCurrentUser();

                    CurrentUser = User.CurrentUser;

                    if (CurrentUser != null)
                    {
                        // Загружаем аватар
                        if (CurrentUser.HasAvatar)
                        {
                            await CurrentUser.LoadAvatarAsync();
                        }
                        else
                        {
                            CurrentUser.CreateDefaultAvatar();
                        }

                        // Уведомляем UI
                        OnPropertyChanged(nameof(CurrentUser));
                    }
                }

                CurrentUser = User.CurrentUser;

                // ИНИЦИАЛИЗАЦИЯ АВАТАРА ПОЛЬЗОВАТЕЛЯ
                await InitializeUserAvatar();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadCurrentUser error: {ex.Message}");
            }
        }

        private async Task InitializeUserAvatar()
        {
            try
            {
                if (CurrentUser == null) return;

                // Получаем инициалы
                UserInitials = GetUserInitials(CurrentUser.DisplayName);

                // Загружаем аватар
                await LoadUserAvatarAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"InitializeUserAvatar error: {ex.Message}");
            }
        }

        private async Task LoadUserAvatarAsync()
        {
            if (CurrentUser == null) return;

            try
            {
                if (string.IsNullOrEmpty(CurrentUser.AvatarUrl))
                {
                    // Нет аватара - создаем дефолтный
                    CreateDefaultAvatar();
                    HasUserAvatar = false;
                    return;
                }

                // Проверяем тип аватара
                if (CurrentUser.AvatarUrl.StartsWith("http"))
                {
                    // Аватар с ImgBB или другого URL
                    await LoadAvatarFromUrlAsync(CurrentUser.AvatarUrl);
                    HasUserAvatar = true;
                }
                else if (File.Exists(CurrentUser.AvatarUrl))
                {
                    // Локальный аватар
                    LoadAvatarFromFile(CurrentUser.AvatarUrl);
                    HasUserAvatar = true;
                }
                else
                {
                    // Файл не найден - создаем дефолтный
                    CreateDefaultAvatar();
                    HasUserAvatar = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadUserAvatarAsync error: {ex.Message}");
                CreateDefaultAvatar();
                HasUserAvatar = false;
            }
        }

        private async Task LoadAvatarFromUrlAsync(string url)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(url + "?t=" + DateTime.Now.Ticks); // Добавляем timestamp для предотвращения кэширования
                bitmap.DecodePixelWidth = 40; // Оптимизируем для маленького размера
                bitmap.EndInit();
                bitmap.Freeze();

                UserAvatarImage = bitmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadAvatarFromUrlAsync error: {ex.Message}");
                CreateDefaultAvatar();
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
                bitmap.DecodePixelWidth = 40; // Оптимизируем для маленького размера
                bitmap.EndInit();
                bitmap.Freeze();

                UserAvatarImage = bitmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadAvatarFromFile error: {ex.Message}");
                CreateDefaultAvatar();
            }
        }

        private void CreateDefaultAvatar()
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
                        UserInitials,
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

                UserAvatarImage = bitmapImage;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CreateDefaultAvatar error: {ex.Message}");
                UserAvatarImage = null;
            }
        }

        // Команда для обновления аватара
        [RelayCommand]
        private async Task RefreshAvatar()
        {
            if (CurrentUser == null) return;

            try
            {
                // Перезагружаем аватар
                await CurrentUser.LoadAvatarAsync();

                // Уведомляем UI
                OnPropertyChanged(nameof(CurrentUser));

                MessageBox.Show("Аватар обновлен", "Информация");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RefreshAvatar error: {ex.Message}");
            }
        }

        private string GetUserInitials(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
                return "U";

            var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return "U";

            if (parts.Length == 1)
                return parts[0].Substring(0, 1).ToUpper();

            return $"{parts[0].Substring(0, 1)}{parts[^1].Substring(0, 1)}".ToUpper();
        }

        private async void InitializeChats()
        {
            try
            {

                if (CurrentUser != null)
                {
                    // Сначала исправляем структуру данных
                    await _firebaseService.FixUserChatsStructureAsync(CurrentUser.Id);

                    // Пробуем загрузить реальные чаты из Firebase
                    var realChats = await _firebaseService.GetUserChatsAsync(CurrentUser.Id);

                    if (realChats.Count > 0)
                    {
                        foreach (var chat in realChats)
                        {
                            Chats.Add(chat);
                        }
                    }
                    else
                    {
                        // Если реальных чатов нет, создаем тестовые

                        CreateTestChats();
                    }

                    // Обновляем отфильтрованные чаты
                    FilteredChats = new ObservableCollection<Chat>(Chats);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка инициализации чатов: {ex.Message}", "Ошибка");
                CreateTestChats();
                FilteredChats = new ObservableCollection<Chat>(Chats);
            }
        }

        private void CreateTestChats()
        {
            Chats.Add(new Chat
            {
                Id = "chat1",
                Name = "Demo Chat 1",
                Type = ChatType.Private,
                LastMessage = new Message
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = "Hello! This is a test message.",
                    SenderName = "System",
                    CreatedAt = DateTime.UtcNow,
                    Status = MessageStatus.Read
                }
            });

            Chats.Add(new Chat
            {
                Id = "chat2",
                Name = "Demo Chat 2",
                Type = ChatType.Private,
                LastMessage = new Message
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = "Welcome to the chat!",
                    SenderName = "Admin",
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                    Status = MessageStatus.Read
                }
            });
        }

        // ========== ФИЛЬТРАЦИЯ ЧАТОВ ==========
        partial void OnSearchTextChanged(string value)
        {
            FilterChats();
        }

        private void FilterChats()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredChats = new ObservableCollection<Chat>(Chats);
                return;
            }

            var searchLower = SearchText.ToLower();
            var filtered = Chats
                .Where(c => c.Name.ToLower().Contains(searchLower) ||
                           (c.LastMessage?.Content?.ToLower().Contains(searchLower) ?? false))
                .ToList();

            FilteredChats = new ObservableCollection<Chat>(filtered);
        }


        [RelayCommand]
        private void OpenProfile()
        {
            try
            {
                var firebaseService = new FirebaseService();
                var localStorage = new LocalStorageService();
                var navigationService = new NavigationService();

                // Можно передать зависимости в конструктор
                navigationService.NavigateToProfile();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия профиля: {ex.Message}", "Ошибка");
            }
        }

        [RelayCommand]
        private void SearchUsers()
        {
            try
            {
                var searchWindow = new SearchWindow();
                searchWindow.Owner = Application.Current.MainWindow;
                searchWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                searchWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия поиска: {ex.Message}", "Ошибка");
            }
        }

        [RelayCommand]
        private void NewChat()
        {
            // Открываем окно поиска пользователей для создания нового чата
            SearchUsers();
        }

        [RelayCommand]
        private void OpenChatMenu()
        {
            if (SelectedChat == null)
                return;

            MessageBox.Show($"Меню чата: {SelectedChat.Name}\n\n" +
                          "Здесь будут доступны:\n" +
                          "• Информация о чате\n" +
                          "• Участники\n" +
                          "• Настройки уведомлений\n" +
                          "• Выход из чата", "Меню чата");
        }


        [RelayCommand]
        private async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(NewMessage) || SelectedChat == null || CurrentUser == null)
                return;

            try
            {
                // 1. Создаем сообщение
                var message = new Message
                {


                    Id = Guid.NewGuid().ToString(),
                    ChatId = SelectedChat.Id,
                    SenderId = CurrentUser.Id,
                    SenderName = CurrentUser.DisplayName,
                    Content = NewMessage.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    Status = MessageStatus.Sent
                };

                // 2. Добавляем в UI
                if (Application.Current.Dispatcher.CheckAccess())
                {
                    Messages.Add(message);
                }
                else
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Messages.Add(message);
                    });
                }

                // 3. Очищаем поле
                NewMessage = string.Empty;

                // 4. Прокручиваем
                ScrollToLastMessage();

                // 5. Отправляем в Firebase
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _firebaseService.SendMessageAsync(message);
                        // Обновляем LastMessage в чате
                        SelectedChat.LastMessage = message;
                        OnPropertyChanged(nameof(SelectedChat));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"❌ Ошибка отправки в Firebase: {ex.Message}", "Error");
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка: {ex.Message}", "Ошибка");
            }
        }




        private async void LoadMessagesForChat(string chatId)
        {
            try
            {
                // Отписываемся от предыдущих сообщений
                _messagesSubscription?.Dispose();
                _messagesSubscription = null;

                // Очищаем сообщения
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Messages.Clear();
                });

                // Загружаем существующие сообщения
                var messages = await _firebaseService.GetChatMessagesAsync(chatId);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var message in messages.OrderBy(m => m.CreatedAt))
                    {
                        Messages.Add(message);
                    }
                });

                ScrollToLastMessage();
                SubscribeToChatMessages(chatId);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка загрузки сообщений: {ex.Message}", "Ошибка");
            }
        }

        private void SubscribeToChatMessages(string chatId)
        {
            try
            {
                _messagesSubscription?.Dispose();

                _messagesSubscription = _firebaseService
                    .ObserveChatMessages(chatId)
                    .Subscribe(firebaseEvent =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            HandleFirebaseEvent(firebaseEvent);
                        });
                    });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Subscribe error: {ex.Message}");
            }
        }

        private void HandleFirebaseEvent(FirebaseEvent<Message> firebaseEvent)
        {
            try
            {
                if (firebaseEvent.Object == null) return;

                var message = firebaseEvent.Object;

                System.Diagnostics.Debug.WriteLine($"=== FIREBASE EVENT ===");
                System.Diagnostics.Debug.WriteLine($"Event type: {firebaseEvent.EventType}");
                System.Diagnostics.Debug.WriteLine($"Message ID: {message.Id}");
                System.Diagnostics.Debug.WriteLine($"Content: '{message.Content}'");
                System.Diagnostics.Debug.WriteLine($"IsDeleted: {message.IsDeleted}");

                // Ищем существующее сообщение
                var existingMessage = Messages.FirstOrDefault(m => m.Id == message.Id);

                switch (firebaseEvent.EventType)
                {
                    case FirebaseEventType.InsertOrUpdate:
                        if (existingMessage != null)
                        {
                            // ОБНОВЛЕНИЕ существующего сообщения
                            System.Diagnostics.Debug.WriteLine($"Updating existing message");

                            // Копируем свойства
                            existingMessage.Content = message.Content;
                            existingMessage.IsEdited = message.IsEdited;
                            existingMessage.IsDeleted = message.IsDeleted;
                            existingMessage.EditedAt = message.EditedAt;
                            existingMessage.DeletedAt = message.DeletedAt;
                            existingMessage.EditedBy = message.EditedBy;
                            existingMessage.DeletedBy = message.DeletedBy;

                            // Принудительно обновляем коллекцию
                            var index = Messages.IndexOf(existingMessage);
                            if (index >= 0)
                            {
                                Messages[index] = existingMessage;
                            }
                        }
                        else
                        {
                            // НОВОЕ сообщение
                            System.Diagnostics.Debug.WriteLine($"Adding new message");

                            // Показываем уведомление
                            if (message.SenderId != CurrentUser?.Id)
                            {
                                ShowMessageNotification(message);
                            }

                            Messages.Add(message);
                            ScrollToLastMessage();

                            // Обновляем LastMessage в чате
                            if (SelectedChat != null)
                            {
                                SelectedChat.LastMessage = message;
                                OnPropertyChanged(nameof(SelectedChat));
                            }
                        }
                        break;

                    case FirebaseEventType.Delete:
                        // УДАЛЕНИЕ сообщения
                        System.Diagnostics.Debug.WriteLine($"Deleting message");
                        if (existingMessage != null)
                        {
                            Messages.Remove(existingMessage);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"HandleFirebaseEvent error: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task DebugAllChats()
        {
            try
            {
                var firebase = new FirebaseService();
                var allChats = await firebase.GetAllChatsAsync(); 

                var info = "=== ВСЕ ЧАТЫ В БАЗЕ ===\n\n";

                foreach (var chat in allChats)
                {
                    info += $"💬 ЧАТ: {chat.Name}\n";
                    info += $"   ID: {chat.Id}\n";
                    info += $"   Тип: {chat.Type}\n";
                    info += $"   Участники: {string.Join(", ", chat.Participants ?? new List<string>())}\n";

                    // Определяем, кто это
                    if (chat.Participants != null && CurrentUser != null)
                    {
                        var otherId = chat.Participants.FirstOrDefault(p => p != CurrentUser.Id);
                        info += $"   Собеседник ID: {otherId}\n";
                    }
                    info += "---\n";
                }

                MessageBox.Show(info, "Все чаты в Firebase");
            }
            catch { }
        }


        [RelayCommand]
        private async Task FixDuplicateChats()
        {
            try
            {
                var firebase = new FirebaseService();
                var allChats = await firebase.GetAllChatsAsync();

                var duplicates = new Dictionary<string, List<Chat>>();

                // Группируем чаты по парам пользователей
                foreach (var chat in allChats)
                {
                    if (chat.Type == ChatType.Private && chat.Participants?.Count == 2)
                    {
                        var key = string.Join("_", chat.Participants.OrderBy(id => id));

                        if (!duplicates.ContainsKey(key))
                            duplicates[key] = new List<Chat>();

                        duplicates[key].Add(chat);
                    }
                }

                // Находим дубликаты
                foreach (var pair in duplicates)
                {
                    if (pair.Value.Count > 1)
                    {
                        MessageBox.Show($"Найдены дубликаты для пары: {pair.Key}\n" +
                                       $"Количество чатов: {pair.Value.Count}",
                                       "Дубликаты");

                        // Оставляем первый чат, остальные удаляем
                        var mainChat = pair.Value.First();
                        for (int i = 1; i < pair.Value.Count; i++)
                        {
                            MessageBox.Show($"Удаляю дубликат: {pair.Value[i].Id}", "Удаление");
                            // Удаляем из Firebase
                            await firebase.DeleteChatAsync(pair.Value[i].Id);
                        }
                    }
                }

                MessageBox.Show("Дубликаты исправлены!", "Успех");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }


        [RelayCommand]
        private void DebugInfo()
        {
            try
            {
                var debugText = $"=== DEBUG INFO ===\n" +
                               $"SelectedChat: {SelectedChat?.Name ?? "NULL"} (ID: {SelectedChat?.Id ?? "NULL"})\n" +
                               $"Messages.Count: {Messages.Count}\n" +
                               $"CurrentUser: {CurrentUser?.Username ?? "NULL"} (ID: {CurrentUser?.Id ?? "NULL"})\n" +
                               $"NewMessage: '{NewMessage}'\n" +
                               $"Chats.Count: {Chats.Count}";

                System.Windows.MessageBox.Show(debugText, "Debug Info");

                // Проверяем каждое сообщение
                if (Messages.Count > 0)
                {
                    var messagesInfo = $"=== MESSAGES ===\n";
                    for (int i = 0; i < Messages.Count; i++)
                    {
                        var msg = Messages[i];
                        messagesInfo += $"\n[{i}] {msg.SenderName}: {msg.Content}\n" +
                                      $"   IsCurrentUser: {msg.IsCurrentUser}, SenderId: {msg.SenderId}";
                    }

                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Debug error: {ex.Message}", "Error");
            }
        }

        [RelayCommand]
        private void TestMessages()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {

                    // Тест 1: Простое сообщение
                    Messages.Add(new Message
                    {
                        Id = Guid.NewGuid().ToString(),
                        Content = "ТЕСТ 1: Простое сообщение",
                        SenderName = "Тест",
                        CreatedAt = DateTime.Now,
                        Status = MessageStatus.Read
                    });

                    // Тест 2: Сообщение от текущего пользователя
                    Messages.Add(new Message
                    {
                        Id = Guid.NewGuid().ToString(),
                        Content = "ТЕСТ 2: От меня",
                        SenderId = CurrentUser?.Id ?? "test",
                        SenderName = CurrentUser?.DisplayName ?? "Вы",
                        CreatedAt = DateTime.Now,
                        Status = MessageStatus.Read,
                        ChatId = SelectedChat?.Id ?? "test"
                    });

                    // Тест 3: Сообщение от другого
                    Messages.Add(new Message
                    {
                        Id = Guid.NewGuid().ToString(),
                        Content = "ТЕСТ 3: От другого",
                        SenderId = "other_user_123",
                        SenderName = "Другой пользователь",
                        CreatedAt = DateTime.Now,
                        Status = MessageStatus.Read,
                        ChatId = SelectedChat?.Id ?? "test"
                    });


                    // Прокручиваем к последнему сообщению
                    ScrollToLastMessage();
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка теста: {ex.Message}", "Error");
            }
        }

        private void ShowMessageNotification(Message message)
        {
            try
            {
                // Показываем уведомление только если:
                // 1. Сообщение не от нас
                // 2. Окно свернуто или не активно
                // 3. Или просто для теста

                if (message.SenderId == CurrentUser?.Id)
                    return; // Не показываем свои сообщения

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                    if (mainWindow != null)
                    {
                        // Проверяем состояние окна
                        bool shouldNotify = mainWindow.WindowState == WindowState.Minimized ||
                                           !mainWindow.IsActive ||
                                           SelectedChat?.Id != message.ChatId;

                        if (shouldNotify)
                        {
                            string preview = message.Content.Length > 30
                                ? message.Content.Substring(0, 30) + "..."
                                : message.Content;

                            mainWindow.ShowNotification($"{message.SenderName}", preview);

                            // Также можно мигать окном в панели задач
                            if (mainWindow.WindowState == WindowState.Minimized)
                            {
                                mainWindow.FlashWindow();
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка уведомления: {ex.Message}");
            }
        }

        private void ScrollToLastMessage()
        {
            try
            {
                // Проверяем доступ к Dispatcher
                if (Application.Current.Dispatcher.CheckAccess())
                {
                    // Прямой вызов
                    var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                    if (mainWindow != null)
                    {
                        var scrollViewer = mainWindow.FindName("MessagesScrollViewer") as ScrollViewer;
                        scrollViewer?.ScrollToEnd();
                    }
                }
                else
                {
                    // Асинхронный вызов через Dispatcher
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                        if (mainWindow != null)
                        {
                            var scrollViewer = mainWindow.FindName("MessagesScrollViewer") as ScrollViewer;
                            scrollViewer?.ScrollToEnd();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка прокрутки: {ex.Message}");
            }
        }



        [RelayCommand]
        private void TestCurrentUser()
        {
            try
            {
                if (CurrentUser == null)
                {
                    System.Windows.MessageBox.Show("CurrentUser is NULL!", "Error");
                    return;
                }

                // Создаем тестовое сообщение ОТ ТЕКУЩЕГО пользователя
                var testMessage = new Message
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = "ТЕСТ: сообщение от текущего пользователя",
                    SenderId = CurrentUser.Id, // Важно: используем ID текущего пользователя
                    SenderName = CurrentUser.DisplayName,
                    CreatedAt = DateTime.UtcNow,
                    Status = MessageStatus.Read,
                    ChatId = SelectedChat?.Id ?? "test"
                };

                // Создаем тестовое сообщение ОТ ДРУГОГО пользователя
                var testMessageOther = new Message
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = "ТЕСТ: сообщение от другого пользователя",
                    SenderId = "OTHER_USER_FAKE_ID_12345", // Другой ID
                    SenderName = "Другой Пользователь",
                    CreatedAt = DateTime.UtcNow,
                    Status = MessageStatus.Read,
                    ChatId = SelectedChat?.Id ?? "test"
                };

                // Добавляем оба сообщения
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Messages.Add(testMessage);
                    Messages.Add(testMessageOther);
                    ScrollToLastMessage();
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Test error: {ex.Message}", "Error");
            }
        }

        [RelayCommand]
        private void Logout()
        {
            try
            {
                var result = System.Windows.MessageBox.Show("Вы уверены, что хотите выйти из аккаунта?",
                    "Подтверждение выхода",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    

                    // Создаем сервисы для выхода
                    var firebaseService = new FirebaseService();
                    var localStorage = new LocalStorageService();
                    var firebaseAuth = new FirebaseAuthService();
                    var authService = new AuthService(firebaseService, localStorage, firebaseAuth);

                    // Выполняем выход
                    authService.Logout();


                    // Отписываемся от сообщений
                    _messagesSubscription?.Dispose();

                    // Закрываем главное окно
                    var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                    mainWindow?.Close();

                    // Открываем окно входа
                    var loginWindow = new LoginWindow();
                    loginWindow.Show();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка выхода: {ex.Message}", "Ошибка");
            }
        }

        partial void OnSelectedChatChanged(Chat? value)
        {
            if (value != null)
            {
                LoadMessagesForChat(value.Id);
            }
            else
            {
                // Очищаем сообщения при снятии выбора
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Messages.Clear();
                });

                // Отписываемся от сообщений
                _messagesSubscription?.Dispose();
                _messagesSubscription = null;
            }
        }
        public void Cleanup()
        {
            // Отписываемся от сообщений при закрытии
            User.AvatarChanged -= OnUserAvatarChanged;
            _messagesSubscription?.Dispose();
            _messagesSubscription = null;
        }

        [RelayCommand]
        private void OpenSearch()
        {
            try
            {
                var firebaseService = new FirebaseService();
                var localStorage = new LocalStorageService();
                var navigationService = new NavigationService();
                var chatService = new ChatService(firebaseService, localStorage);

                navigationService.NavigateToSearch();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия поиска: {ex.Message}", "Ошибка");
            }
        }

        [ObservableProperty]
        private bool _isDarkTheme = ThemeManager.IsDarkTheme;

        [RelayCommand]
        private void OpenSettings()
        {
            // Простое переключение темы
            if (Messenger.Themes.ThemeManager.IsDarkTheme)
            {
                // Переключаем на светлую
                ThemeManager.ApplyLightTheme(Application.Current.Resources);
                Application.Current.MainWindow.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            }
            else
            {
                // Переключаем на темную
                ThemeManager.ApplyDarkTheme(Application.Current.Resources);
                Application.Current.MainWindow.Background = new SolidColorBrush(Color.FromRgb(30, 30, 35));
            }

            // Обновляем свойство в VM для привязок
            IsDarkTheme = ThemeManager.IsDarkTheme;

            // Сообщение о текущей теме
            var themeName = ThemeManager.IsDarkTheme ? "Темная" : "Светлая";
            MessageBox.Show($"Тема изменена на: {themeName}", "Настройки");
        }
    }
}