using System.Windows;
using System.Windows.Input;
using Messenger.Services;
using Messenger.ViewModels;

namespace Messenger.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            Loaded += LoginWindow_Loaded;
        }

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var firebaseService = new FirebaseService();
            var localStorage = new LocalStorageService();
            var firebaseAuth = new FirebaseAuthService();
            var navigationService = new NavigationService();
            var authService = new AuthService(firebaseService, localStorage, firebaseAuth);

            this.DataContext = new LoginViewModel(authService, navigationService);

            UsernameTextBox.Focus();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel)
            {
                viewModel.Password = PasswordBox.Password;
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // При нажатии Enter в поле пароля - логинимся
                if (sender == PasswordBox && !string.IsNullOrEmpty(UsernameTextBox.Text))
                {
                    var viewModel = DataContext as LoginViewModel;
                    viewModel?.LoginCommand.Execute(null);
                }
            }
        }
    }
}