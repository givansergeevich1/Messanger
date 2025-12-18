using Messenger.Models;
using Messenger.Views;
using System.Windows;

namespace Messenger.Services
{
    public class NavigationService
    {
        public void NavigateToLogin()
        {
            try
            {
                var loginWindow = new LoginWindow();
                loginWindow.Show();

                Application.Current.MainWindow?.Close();
                Application.Current.MainWindow = loginWindow;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Navigation error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void NavigateToRegister()
        {
            try
            {
                var registerWindow = new RegisterWindow();
                registerWindow.Show();

                Application.Current.MainWindow?.Close();
                Application.Current.MainWindow = registerWindow;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Navigation error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void NavigateToMain()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = new MainWindow();
                    mainWindow.Show();

                    var currentWindow = Application.Current.MainWindow;
                    if (currentWindow != null && currentWindow != mainWindow)
                    {
                        currentWindow.Close();
                    }

                    Application.Current.MainWindow = mainWindow;
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка навигации: {ex.Message}", "Ошибка");
            }
        }

        public void NavigateToProfile()
        {
            try
            {
                var profileWindow = new ProfileWindow();
                profileWindow.Owner = Application.Current.MainWindow;
                profileWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                profileWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия профиля: {ex.Message}", "Ошибка");
            }
        }


        public void NavigateToSearch()
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

        public void NavigateToChat(Chat chat)
        {
            try
            {
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия чата: {ex.Message}", "Ошибка");
            }
        }

        public void ShowMessage(string title, string message, MessageBoxImage icon = MessageBoxImage.Information)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(message, title, MessageBoxButton.OK, icon);
                });
            }
            catch { }
        }

        public bool ShowConfirmation(string title, string message)
        {
            try
            {
                var result = MessageBoxResult.No;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
                });

                return result == MessageBoxResult.Yes;
            }
            catch
            {
                return false;
            }
        }
    }
}