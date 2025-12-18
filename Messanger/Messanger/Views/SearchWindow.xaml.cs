using System.Windows;
using Messenger.Services;
using Messenger.ViewModels;

namespace Messenger.Views
{
    public partial class SearchWindow : Window
    {
        public SearchWindow()
        {
            InitializeComponent();
            Loaded += SearchWindow_Loaded;
        }

        private void SearchWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var firebaseService = new FirebaseService();
            var localStorage = new LocalStorageService();
            var chatService = new ChatService(firebaseService, localStorage);
            var navigationService = new NavigationService();

            this.DataContext = new SearchViewModel(firebaseService, chatService, navigationService);

            SearchTextBox.Focus();
        }
    }
}