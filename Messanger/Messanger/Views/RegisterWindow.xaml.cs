using Messenger.Services;
using Messenger.ViewModels;
using System.Windows;

namespace Messenger.Views
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
            Loaded += RegisterWindow_Loaded;
        }


        private void RegisterWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.MessageBox.Show("1. Начинаем загрузку", "Debug");

                var firebaseAuth = new FirebaseAuthService();
                System.Windows.MessageBox.Show("2. FirebaseAuthService создан", "Debug");

                var firebaseService = new FirebaseService();
                System.Windows.MessageBox.Show("3. FirebaseService создан", "Debug");

                var localStorage = new LocalStorageService();
                var navigationService = new NavigationService();

                var authService = new AuthService(firebaseService, localStorage, firebaseAuth);
                System.Windows.MessageBox.Show("4. AuthService создан", "Debug");

                var viewModel = new RegisterViewModel(authService, navigationService);
                this.DataContext = viewModel;

                System.Windows.MessageBox.Show("5. ViewModel установлен", "Debug");

                UsernameTextBox.Focus();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"ОШИБКА: {ex.Message}\n{ex.StackTrace}", "Critical Error");
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is RegisterViewModel viewModel)
            {
                viewModel.Password = PasswordBox.Password;
            }
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is RegisterViewModel viewModel)
            {
                viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
            }
        }

    }
}