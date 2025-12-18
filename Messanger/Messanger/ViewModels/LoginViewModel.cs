using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Messenger.Services;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Messenger.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly AuthService _authService;
        private readonly NavigationService _navigationService;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isLoading = false;

        public LoginViewModel(AuthService authService, NavigationService navigationService)
        {
            _authService = authService;
            _navigationService = navigationService;
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Пожалуйста, введите логин и пароль";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                System.Windows.MessageBox.Show($"1. Начинаем вход для: {Username}", "Debug Login");

                var user = await _authService.LoginAsync(Username, Password);

                System.Windows.MessageBox.Show($"2. LoginAsync результат: {user != null}", "Debug Login");

                if (user != null)
                {
                    System.Windows.MessageBox.Show($"3. Успешный вход! Пользователь: {user.DisplayName}", "Debug Login");

                    System.Windows.MessageBox.Show($"Добро пожаловать, {user.DisplayName}!",
                        "Успешный вход", MessageBoxButton.OK, MessageBoxImage.Information);

                    System.Windows.MessageBox.Show($"4. Перед NavigateToMain()", "Debug Login");

                    _navigationService.NavigateToMain();

                    System.Windows.MessageBox.Show($"5. После NavigateToMain()", "Debug Login");
                }
                else
                {
                    System.Windows.MessageBox.Show($"6. Вход не удался", "Debug Login");
                    ErrorMessage = "Неверный логин или пароль";
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"7. Ошибка входа: {ex.Message}\n{ex.StackTrace}", "ERROR Login");
                ErrorMessage = $"Ошибка входа: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                System.Windows.MessageBox.Show($"8. Загрузка завершена", "Debug Login");
            }
        }

        [RelayCommand]
        private void NavigateToRegister()
        {
            _navigationService.NavigateToRegister();
        }

        [RelayCommand]
        private async Task ForgotPassword()
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                MessageBox.Show("Введите ваш email для восстановления пароля",
                    "Восстановление пароля", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var result = await _authService.ResetPasswordAsync(Username);

                if (result)
                {
                    MessageBox.Show("Ссылка для сброса пароля отправлена на вашу почту",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Не удалось отправить ссылку для сброса пароля",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}