    using Messenger.Models;
    using System.Windows;

    namespace Messenger.Services
    {
        public class ChatService
        {
            private readonly FirebaseService _firebaseService;
            private User? _currentUser;

            public ChatService(FirebaseService firebaseService, LocalStorageService localStorage)
            {
                _firebaseService = firebaseService;
                _currentUser = User.CurrentUser ?? localStorage.GetCurrentUser();
            }

            public async Task<Chat> CreatePrivateChatAsync(string participantId, string participantName)
            {
                if (_currentUser == null)
                    throw new Exception("User not logged in");


                if (_currentUser.Id == participantId)
                {
                    MessageBox.Show("❌ КРИТИЧЕСКАЯ ОШИБКА: Вы передаете СВОЙ ID как ID участника!", "ОШИБКА");
                    throw new Exception("Cannot create chat with yourself");
                }

                await _firebaseService.DebugShowAllChats();


                var existingChat = await _firebaseService.FindExistingPrivateChatAsync(_currentUser.Id, participantId);

                if (existingChat != null)
                {

                    await _firebaseService.AddUserToChatAsync(_currentUser.Id, existingChat.Id);
                    return existingChat;
                }
                string chatId = GenerateConsistentChatId(_currentUser.Id, participantId);

                var chat = new Chat
                {
                    Id = chatId,
                    Type = ChatType.Private,
                    Name = participantName, // Имя собеседника
                    CreatedById = _currentUser.Id,
                    Participants = new List<string> { _currentUser.Id, participantId },
                    CreatedAt = DateTime.UtcNow,
                    LastMessage = null
                };

                MessageBox.Show($"Будет сохранен чат:\n" +
                               $"ID: {chat.Id}\n" +
                               $"Название: {chat.Name}\n" +
                               $"Участники: {string.Join(", ", chat.Participants)}", "Debug");
                var createdChat = await _firebaseService.CreateChatAsync(chat);

                await _firebaseService.AddUserToChatAsync(_currentUser.Id, chatId);
                await _firebaseService.AddUserToChatAsync(participantId, chatId);

                MessageBox.Show($"✅ Чат '{participantName}' создан и добавлен обоим пользователям", "Успех");

                return createdChat;
            }

            private string GenerateConsistentChatId(string userId1, string userId2)
            {
                var ids = new List<string> { userId1, userId2 };
                ids.Sort(); // Сортируем, чтобы ID1_ID2 и ID2_ID1 давали одинаковый результат

                // Создаем ID на основе отсортированных ID
                return $"private_{string.Join("_", ids)}";
            }

            private async Task EnsureChatInUserChatsAsync(string userId, string chatId)
            {
                try
                {
                    // Используем новый метод или реализуем логику
                    await _firebaseService.AddUserToChatAsync(userId, chatId);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"⚠️ Ошибка добавления чата пользователю {userId}: {ex.Message}", "Внимание");
                }
            }

            public async Task<Chat> CreateGroupChatAsync(string chatName, List<string> participantIds)
            {
                if (_currentUser == null)
                    throw new Exception("User not logged in");

                var allParticipants = new List<string>(participantIds) { _currentUser.Id };

                var chat = new Chat
                {
                    Type = ChatType.Group,
                    Name = chatName,
                    CreatedById = _currentUser.Id,
                    Participants = allParticipants
                };

                Console.WriteLine($"Creating group chat: {chatName} with {participantIds.Count} participants");
                return await _firebaseService.CreateChatAsync(chat);
            }

        public async Task SendMessageAsync(string chatId, string content, MessageType type = MessageType.Text, string? fileUrl = null, string? fileName = null, long fileSize = 0)
        {
            if (_currentUser == null)
            {
                Console.WriteLine("Cannot send message: user not logged in");
                return;
            }

            var message = new Message
            {
                ChatId = chatId,
                SenderId = _currentUser.Id,
                SenderName = _currentUser.DisplayName,
                Content = content,
                MessageType = type,
                CreatedAt = DateTime.UtcNow,
                Status = MessageStatus.Sent
            };

            if (!string.IsNullOrEmpty(fileUrl) || !string.IsNullOrEmpty(fileName))
            {
                message.FileAttachment = new FileAttachment
                {
                    Url = fileUrl ?? string.Empty,
                    FileName = fileName ?? string.Empty,
                    FileSize = fileSize
                };
            }

            Console.WriteLine($"Sending message to chat {chatId}: {content.Substring(0, Math.Min(content.Length, 50))}...");
            await _firebaseService.SendMessageAsync(message);
        }

        public async Task<List<User>> SearchUsersAsync(string searchTerm)
            {
                Console.WriteLine($"Searching users with term: {searchTerm}");
                return new List<User>();
            }

            public async Task UpdateMessageStatusAsync(string chatId, string messageId, MessageStatus status)
            {
                Console.WriteLine($"Updating message {messageId} status to {status}");

                await Task.CompletedTask;
            }
        }
    }
