using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Messenger.Models;

namespace Messenger.Converters
{
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter?.ToString() == "Inverse")
            {
                return string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;
            }
            return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is bool isCurrentUser)
                {
                    var alignment = isCurrentUser ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                    System.Diagnostics.Debug.WriteLine($"AlignmentConverter: isCurrentUser={isCurrentUser}");
                    return alignment;
                }
                System.Diagnostics.Debug.WriteLine($"AlignmentConverter: value is not bool");
                return HorizontalAlignment.Left;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AlignmentConverter error: {ex.Message}");
                return HorizontalAlignment.Left;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class AlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isCurrentUser)
            {
                return isCurrentUser ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            }
            return HorizontalAlignment.Left;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is HorizontalAlignment alignment)
            {
                return alignment == HorizontalAlignment.Right;
            }
            return false;
        }
    }

    public class MessageStatusToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MessageStatus status)
            {
                return status switch
                {
                    MessageStatus.Sending => "⏳",
                    MessageStatus.Sent => "✓",
                    MessageStatus.Delivered => "✓✓",
                    MessageStatus.Read => "✓✓",
                    MessageStatus.Failed => "✗",
                    _ => "⏳"
                };
            }
            return "⏳";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MessageStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MessageStatus status)
            {
                return status switch
                {
                    MessageStatus.Sending => Brushes.Gray,
                    MessageStatus.Sent => Brushes.Gray,
                    MessageStatus.Delivered => Brushes.Orange,
                    MessageStatus.Read => Brushes.Green,
                    MessageStatus.Failed => Brushes.Red,
                    _ => Brushes.Gray
                };
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MessageBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isCurrentUser)
            {
                if (isCurrentUser)
                {
                    // Мои сообщения
                    return Application.Current.FindResource("MessageBackgroundSelf")
                           ?? new SolidColorBrush(Color.FromArgb(255, 0, 120, 215));
                }
                else
                {
                    // Сообщения других
                    return Application.Current.FindResource("MessageBackgroundOther")
                           ?? new SolidColorBrush(Color.FromArgb(255, 240, 240, 240));
                }
            }
            return new SolidColorBrush(Color.FromArgb(255, 240, 240, 240));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MessageTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isCurrentUser)
            {
                return isCurrentUser ? Brushes.White : Brushes.Black;
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MessageTimeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isCurrentUser)
            {
                return isCurrentUser ?
                    new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)) :
                    new SolidColorBrush(Color.FromArgb(150, 0, 0, 0));
            }
            return new SolidColorBrush(Color.FromArgb(150, 0, 0, 0));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class UserStatusToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is UserStatus status)
            {
                return status switch
                {
                    UserStatus.Online => "🟢 Онлайн",
                    UserStatus.Offline => "⚫ Офлайн",
                    UserStatus.DoNotDisturb => "⛔ Не беспокоить",
                    _ => "⚫ Офлайн"
                };
            }
            return "⚫ Офлайн";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string statusString)
            {
                return statusString switch
                {
                    "🟢 Онлайн" => UserStatus.Online,
                    "⚫ Офлайн" => UserStatus.Offline,
                    "⛔ Не беспокоить" => UserStatus.DoNotDisturb,
                    _ => UserStatus.Offline
                };
            }
            return UserStatus.Offline;
        }
    }

    public class UserStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is UserStatus status)
            {
                return status switch
                {
                    UserStatus.Online => Brushes.Green,
                    UserStatus.Offline => Brushes.Gray,
                    UserStatus.DoNotDisturb => Brushes.Orange,
                    _ => Brushes.Gray
                };
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ChatTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ChatType type)
            {
                return type switch
                {
                    ChatType.Private => "👤",      // Приватный чат
                    ChatType.Group => "👥",        // Групповой чат
                    _ => "💬"                      // По умолчанию
                };
            }
            return "💬";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ChatTypeToColorConverter : IMultiValueConverter
    {
        private static readonly Color[] _colors = new[]
        {
        Color.FromRgb(66, 133, 244),   // Синий
        Color.FromRgb(219, 68, 55),    // Красный
        Color.FromRgb(244, 180, 0),    // Жёлтый
        Color.FromRgb(15, 157, 88),    // Зелёный
        Color.FromRgb(171, 71, 188),   // Фиолетовый
        Color.FromRgb(0, 172, 193),    // Бирюзовый
    };

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is ChatType type && values[1] is string id)
            {
                // Генерация цвета на основе ID чата
                int colorIndex = Math.Abs(id.GetHashCode()) % _colors.Length;
                return new SolidColorBrush(_colors[colorIndex]);
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count && count > 0)
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FileTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string fileType)
            {
                return fileType switch
                {
                    "image" => "🖼️",
                    "document" => "📄",
                    "audio" => "🎵",
                    "video" => "🎬",
                    _ => "📎"
                };
            }
            return "📎";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long bytes)
            {
                string[] sizes = { "B", "KB", "MB", "GB" };
                double len = bytes;
                int order = 0;

                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }

                return $"{len:0.##} {sizes[order]}";
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class MessageContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Message message)
            {
                if (message.IsDeleted)
                    return "🗑️ Сообщение удалено";

                if (message.IsEdited)
                    return $"{message.Content} (ред.)";

                return message.Content;
            }

            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MessageOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Message message)
            {
                return message.IsDeleted ? 0.6 : 1.0;
            }

            return 1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class IsDeletedToStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== IsDeletedToStyleConverter called ===");

                if (value is bool isDeleted && isDeleted)
                {
                    System.Diagnostics.Debug.WriteLine("Message is deleted, looking for style...");

                    var window = Application.Current?.MainWindow;
                    if (window != null)
                    {
                        var style = window.TryFindResource("DeletedMessageStyle");
                        if (style != null)
                        {
                            System.Diagnostics.Debug.WriteLine("Style found in MainWindow resources");
                            return style;
                        }
                    }

                    var appStyle = Application.Current?.TryFindResource("DeletedMessageStyle");
                    if (appStyle != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Style found in Application resources");
                        return appStyle;
                    }

                    System.Diagnostics.Debug.WriteLine("Style not found, creating dynamic style");
                    var dynamicStyle = new Style(typeof(Border));
                    dynamicStyle.Setters.Add(new Setter(Border.OpacityProperty, 0.6));
                    dynamicStyle.Setters.Add(new Setter(Border.BackgroundProperty,
                        new SolidColorBrush(Color.FromArgb(255, 240, 240, 240))));
                    return dynamicStyle;
                }

                System.Diagnostics.Debug.WriteLine("Message is not deleted, returning null");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IsDeletedToStyleConverter ERROR: {ex.Message}");
                return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToFontStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue)
            {
                return FontStyles.Italic;
            }
            return FontStyles.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ThemeNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isDarkTheme)
            {
                return isDarkTheme ? "Темная тема" : "Светлая тема";
            }
            return "Светлая тема";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LeftPanelBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isDarkTheme)
            {
                return isDarkTheme ?
                    new SolidColorBrush(Color.FromRgb(40, 40, 45)) : // Тёмный
                    new SolidColorBrush(Color.FromRgb(248, 249, 250)); // Светлый
            }

            return Messenger.Themes.ThemeManager.IsDarkTheme ?
                new SolidColorBrush(Color.FromRgb(40, 40, 45)) :
                new SolidColorBrush(Color.FromRgb(248, 249, 250));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InitialsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string displayName && !string.IsNullOrEmpty(displayName))
            {
                var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    return "??";

                if (parts.Length == 1)
                    return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();

                return $"{parts[0].Substring(0, 1)}{parts[^1].Substring(0, 1)}".ToUpper();
            }
            return "??";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }


    }

    public class AvailabilityToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isAvailable)
            {
                return isAvailable ?
                    new SolidColorBrush(Color.FromRgb(76, 175, 80)) : // Зелёный
                    new SolidColorBrush(Color.FromRgb(244, 67, 54));  // Красный
            }

            return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Серый
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class AvailabilityToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isAvailable)
            {
                return isAvailable ? "✅" : "❌";
            }

            return "❓";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IsEditingToReadOnlyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEditing)
            {
                if (parameter?.ToString() == "Inverse")
                {
                    return !isEditing;
                }
                return isEditing;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IsCheckingToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecking)
            {
                return isChecking ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EmailValidationConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values.Length >= 2 && values[0] is string email)
                {
                    var originalEmail = values[1] as string;
                    if (email == originalEmail)
                        return new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    if (IsValidEmail(email))
                        return new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    else
                        return new SolidColorBrush(Color.FromRgb(244, 67, 54));
                }
            }
            catch { }

            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

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

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FieldValidationIconConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values.Length >= 2)
                {
                    var isAvailable = values[0] as bool? ?? false;
                    var isChecking = values[1] as bool? ?? false;

                    if (isChecking)
                        return "🔍";

                    return isAvailable ? "✅" : "❌";
                }
            }
            catch { }

            return "❓";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FieldValidationTextConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values.Length >= 2)
                {
                    var isAvailable = values[0] as bool? ?? false;
                    var isChecking = values[1] as bool? ?? false;

                    if (isChecking)
                        return "Проверка...";

                    return isAvailable ? "Доступно" : "Занято";
                }
            }
            catch { }

            return "";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RequiredFieldConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (string.IsNullOrWhiteSpace(value as string))
            {
                return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Красный для пустого поля
            }

            return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Зелёный для заполненного
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}