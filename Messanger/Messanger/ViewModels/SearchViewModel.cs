using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Messenger.Models;
using Messenger.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace Messenger.ViewModels
{
    public partial class SearchViewModel : ObservableObject
    {
        private readonly FirebaseService _firebaseService;
        private readonly ChatService _chatService;
        private readonly NavigationService _navigationService;

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private User? _selectedUser;

        [ObservableProperty]
        private ObservableCollection<User> _searchResults = new();

        [ObservableProperty]
        private bool _isSearching = false;

        public SearchViewModel(
            FirebaseService firebaseService,
            ChatService chatService,
            NavigationService navigationService)
        {
            _firebaseService = firebaseService;
            _chatService = chatService;
            _navigationService = navigationService;
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery) || SearchQuery.Length < 2)
            {
                MessageBox.Show("Введите минимум 2 символа для поиска", "Информация");
                return;
            }

            IsSearching = true;
            SearchResults.Clear();

            try
            {
                // Ищем пользователей
                var users = await _firebaseService.SearchUsersByUsernameAsync(SearchQuery);

                // Фильтруем текущего пользователя из результатов
                foreach (var user in users)
                {
                    if (user.Id != User.CurrentUser?.Id)
                    {
                        SearchResults.Add(user);
                    }
                }

                if (SearchResults.Count == 0)
                {
                    MessageBox.Show("Пользователи не найдены", "Информация");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска: {ex.Message}", "Ошибка");
            }
            finally
            {
                IsSearching = false;
            }
        }

        [RelayCommand]
        private async Task StartChatAsync(User? user)
        {
            if (user == null || User.CurrentUser == null)
                return;

            try
            {

                MessageBox.Show($"=== SearchViewModel.StartChatAsync ===\n\n" +
                       $"ВЫ: {User.CurrentUser.DisplayName} (ID: {User.CurrentUser.Id})\n" +
                       $"ДРУГ: {user.DisplayName} (ID: {user.Id})\n\n" +
                       $"Передаю в ChatService:\n" +
                       $"• participantId = {user.Id}\n" +
                       $"• participantName = {user.DisplayName}",
                       "ОТЛАДКА SearchViewModel");

                // Создаем приватный чат
                var chat = await _chatService.CreatePrivateChatAsync(user.Id, user.DisplayName);

                MessageBox.Show($"Чат с {user.DisplayName} создан!", "Успех");

                // Закрываем окно поиска
                _navigationService.NavigateToMain();

                // TODO: Открыть созданный чат
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания чата: {ex.Message}", "Ошибка");
            }
        }



        [RelayCommand]
        private void CloseSearch()
        {
            _navigationService.NavigateToMain();
        }
    }
}