using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Messenger.Models;
using Messenger.Services;

namespace Messenger.ViewModels
{
    public partial class ChatViewModel : ObservableObject
    {
        private readonly FirebaseService _firebaseService;
        private readonly ChatService _chatService;
        private readonly LocalStorageService _localStorage;

        [ObservableProperty]
        private Chat? _currentChat;

        [ObservableProperty]
        private ObservableCollection<Message> _messages = new();

        [ObservableProperty]
        private string _newMessage = string.Empty;

        [ObservableProperty]
        private bool _isSending;

        private User? _currentUser;
        private IDisposable? _messageSubscription;

        public ChatViewModel(
            FirebaseService firebaseService,
            ChatService chatService,
            LocalStorageService localStorage)
        {
            _firebaseService = firebaseService;
            _chatService = chatService;
            _localStorage = localStorage;
            _currentUser = User.CurrentUser ?? localStorage.GetCurrentUser();
        }

        public async void LoadChat(Chat chat)
        {
            CurrentChat = chat;
            Messages.Clear();

            _messageSubscription?.Dispose();

            Console.WriteLine($"Loading chat: {chat.Name} ({chat.Id})");

            var existingMessages = await _firebaseService.GetChatMessagesAsync(chat.Id);

            if (existingMessages.Count == 0 && _currentUser != null)
            {
                existingMessages.Add(new Message
                {
                    Id = "msg-1",
                    ChatId = chat.Id,
                    SenderId = "system",
                    SenderName = "System",
                    Content = "Welcome to the chat!",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                    Status = MessageStatus.Read
                });

                existingMessages.Add(new Message
                {
                    Id = "msg-2",
                    ChatId = chat.Id,
                    SenderId = _currentUser.Id,
                    SenderName = _currentUser.DisplayName,
                    Content = "Hello! This is a test message.",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-2),
                    Status = MessageStatus.Read
                });
            }

            Messages = new ObservableCollection<Message>(existingMessages.OrderBy(m => m.CreatedAt));

            try
            {
                _messageSubscription = _firebaseService
                    .ObserveChatMessages(chat.Id)
                    .Subscribe(firebaseEvent =>
                    {
                        if (firebaseEvent.Object != null)
                        {
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                var existing = Messages.FirstOrDefault(m => m.Id == firebaseEvent.Object.Id);
                                if (existing == null)
                                {
                                    Messages.Add(firebaseEvent.Object);
                                    Console.WriteLine($"New message received: {firebaseEvent.Object.Content}");
                                }
                            });
                        }
                    });

                Console.WriteLine($"Subscribed to chat {chat.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error subscribing to chat: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(NewMessage) || CurrentChat == null || _currentUser == null)
                return;

            if (NewMessage.Trim().Length == 0)
                return;

            IsSending = true;

            try
            {
                await _chatService.SendMessageAsync(CurrentChat.Id, NewMessage.Trim());
                NewMessage = string.Empty;

                Console.WriteLine($"Message sent to chat {CurrentChat.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
                System.Windows.MessageBox.Show($"Failed to send message: {ex.Message}",
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsSending = false;
            }
        }

        [RelayCommand]
        private void AttachFile()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Select file to attach",
                    Filter = "All files (*.*)|*.*",
                    Multiselect = false
                };

                if (dialog.ShowDialog() == true)
                {
                    NewMessage = $"📎 File: {System.IO.Path.GetFileName(dialog.FileName)}";
                    Console.WriteLine($"File selected: {dialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error attaching file: {ex.Message}");
                System.Windows.MessageBox.Show($"Failed to attach file: {ex.Message}",
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void EditMessage(Message message)
        {
            if (message == null || message.SenderId != _currentUser?.Id) return;

            var newText = Microsoft.VisualBasic.Interaction.InputBox(
                "Edit your message:",
                "Edit Message",
                message.Content);

            if (!string.IsNullOrWhiteSpace(newText) && newText != message.Content)
            {
                message.Content = newText;
                message.IsEdited = true;
                Console.WriteLine($"Message edited: {message.Id}");
            }
        }

        [RelayCommand]
        private async Task DeleteMessage(Message message)
        {
            if (message == null) return;

            var result = System.Windows.MessageBox.Show(
                "Are you sure you want to delete this message?\nThis action cannot be undone.",
                "Confirm Delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                message.IsDeleted = true;
                message.Content = "This message was deleted";

                Console.WriteLine($"Message deleted: {message.Id}");
            }
        }

        public void Cleanup()
        {
            _messageSubscription?.Dispose();
            _messageSubscription = null;
        }
    }
}