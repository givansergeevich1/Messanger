using System.Windows;
using Messenger.ViewModels;
using Messenger.Services;

namespace Messenger.Views
{
    public partial class ProfileWindow : Window
    {
        public ProfileWindow()
        {
            InitializeComponent();
            Loaded += ProfileWindow_Loaded;
        }

        private void ProfileWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Инициализируем ViewModel с зависимостями
            var firebaseService = new FirebaseService();
            var localStorage = new LocalStorageService();
            var navigationService = new NavigationService();

            this.DataContext = new ProfileViewModel(firebaseService, localStorage, navigationService);
        }
    }
}