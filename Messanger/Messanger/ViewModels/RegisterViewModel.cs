using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Messenger.Services;

namespace Messenger.ViewModels
{
    public partial class RegisterViewModel : ObservableObject
    {
        private readonly AuthService _authService;
        private readonly NavigationService _navigationService;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _displayName = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isLoading = false;

        public RegisterViewModel(AuthService authService, NavigationService navigationService)
        {
            _authService = authService;
            _navigationService = navigationService;
        }

        [RelayCommand]
        private async Task RegisterAsync()
        {
            try
            {
                System.Windows.MessageBox.Show($"1. Команда RegisterCommand началась!", "Debug");

                System.Windows.MessageBox.Show($"Проверяем поля:\nUsername: '{Username}'\nEmail: '{Email}'\nPassword: '{Password}'", "Debug");

                // Проверка 1: заполнены ли все поля
                if (string.IsNullOrWhiteSpace(Username))
                {
                    System.Windows.MessageBox.Show("ОШИБКА: Username пустой!", "Debug");
                    ErrorMessage = "Please fill all required fields";
                    return;
                }

                if (string.IsNullOrWhiteSpace(Email))
                {
                    System.Windows.MessageBox.Show("ОШИБКА: Email пустой!", "Debug");
                    ErrorMessage = "Please fill all required fields";
                    return;
                }

                if (string.IsNullOrWhiteSpace(Password))
                {
                    System.Windows.MessageBox.Show("ОШИБКА: Password пустой!", "Debug");
                    ErrorMessage = "Please fill all required fields";
                    return;
                }

                System.Windows.MessageBox.Show($"2. Все поля заполнены!", "Debug");

                // Проверка 2: совпадают ли пароли
                if (Password != ConfirmPassword)
                {
                    System.Windows.MessageBox.Show($"ОШИБКА: Пароли не совпадают!\nPassword: '{Password}'\nConfirm: '{ConfirmPassword}'", "Debug");
                    ErrorMessage = "Passwords do not match";
                    return;
                }

                System.Windows.MessageBox.Show($"3. Пароли совпадают!", "Debug");

                // Проверка 3: длина пароля
                if (Password.Length < 6)
                {
                    System.Windows.MessageBox.Show($"ОШИБКА: Пароль слишком короткий! Длина: {Password.Length}", "Debug");
                    ErrorMessage = "Password must be at least 6 characters";
                    return;
                }

                System.Windows.MessageBox.Show($"4. Все проверки пройдены!", "Debug");

                IsLoading = true;
                ErrorMessage = string.Empty;

                System.Windows.MessageBox.Show($"5. Начинаем регистрацию в AuthService...", "Debug");

                try
                {
                    var user = await _authService.RegisterAsync(
                        Username,
                        Password,
                        Email,
                        DisplayName);

                    System.Windows.MessageBox.Show($"6. Регистрация завершена, результат: {user != null}", "Debug");

                    if (user != null)
                    {
                        System.Windows.MessageBox.Show($"Регистрация успешна!\nДобро пожаловать, {user.DisplayName}!",
                            "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                        _navigationService.NavigateToMain();
                        return;
                    }
                    else
                    {
                        ErrorMessage = "Registration failed. Username may already exist.";
                        System.Windows.MessageBox.Show($"ОШИБКА: {ErrorMessage}", "Debug");
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"ОШИБКА в AuthService: {ex.Message}\n{ex.StackTrace}", "ERROR");
                    ErrorMessage = $"Registration failed: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                    System.Windows.MessageBox.Show("7. Загрузка завершена", "Debug");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"ОШИБКА в команде: {ex.Message}\n{ex.StackTrace}", "CRITICAL ERROR");
            }


        }

        [RelayCommand]
        private void NavigateToLogin()
        {
            _navigationService.NavigateToLogin();
        }
    }
}
