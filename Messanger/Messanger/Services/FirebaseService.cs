using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using Messenger.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.IO;

namespace Messenger.Services
{
    public class FirebaseService
    {
        private const string FirebaseUrl = "https://messenger-d80c5-default-rtdb.asia-southeast1.firebasedatabase.app";
        private FirebaseClient _firebaseClient;

        public FirebaseService()
        {
            try
            {
                _firebaseClient = new FirebaseClient(FirebaseUrl);
                Console.WriteLine("Firebase Service initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Firebase initialization error: {ex.Message}");
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                await _firebaseClient.Child("test").OnceAsync<object>();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // User operations

        public async Task<User?> GetUserAsync(string userId)
        {
            try
            {
                var user = await _firebaseClient
                    .Child("users")
                    .Child(userId)
                    .OnceSingleAsync<User>();
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetUser error: {ex.Message}");
                return null;
            }
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        {
            try
            {
                return await _firebaseClient
                    .Child("users")
                    .Child(userId)
                    .OnceSingleAsync<User>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetUserById error: {ex.Message}");
                return null;
            }
        }

        public async Task<User?> GetUserByUidAsync(string uid)
        {
            try
            {

                var allUsers = await _firebaseClient
                    .Child("users")
                    .OnceAsync<User>();

                foreach (var user in allUsers)
                {
                    if (user.Object.Uid == uid || user.Object.Id == uid)
                    {
                        return user.Object;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ОШИБКА поиска по UID: {ex.Message}", "Error");
                return null;
            }
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            try
            {
                

                var allUsers = await _firebaseClient
                    .Child("users")
                    .OnceAsync<User>();

                

                foreach (var user in allUsers)
                {
                    if (user.Object.Username.ToLower() == username.ToLower())
                    {
                        return user.Object;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ОШИБКА поиска пользователя: {ex.Message}", "Error");
                return null;
            }
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {

                var allUsers = await _firebaseClient
                    .Child("users")
                    .OnceAsync<User>();

                foreach (var user in allUsers)
                {
                    if (user.Object.Email.ToLower() == email.ToLower())
                    {
                        return user.Object;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ОШИБКА поиска по email: {ex.Message}", "Error");
                return null;
            }
        }

        public async Task<User?> GetUserByEmailOrUsernameAsync(string emailOrUsername)
        {
            try
            {

                var allUsers = await _firebaseClient
                    .Child("users")
                    .OnceAsync<User>();

                foreach (var user in allUsers)
                {
                    if (user.Object.Email.ToLower() == emailOrUsername.ToLower() ||
                        user.Object.Username.ToLower() == emailOrUsername.ToLower())
                    {
                        return user.Object;
                    }
                }

                MessageBox.Show($"❌ Пользователь не найден", "Debug");
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ОШИБКА поиска: {ex.Message}", "Error");
                return null;
            }
        }

        public async Task<List<User>> SearchUsersByUsernameAsync(string searchTerm)
        {
            var users = new List<User>();
            try
            {
                var allUsers = await _firebaseClient
                    .Child("users")
                    .OnceAsync<User>();

                foreach (var user in allUsers)
                {
                    if (user.Object.Username.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        user.Object.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    {
                        users.Add(user.Object);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SearchUsersByUsername error: {ex.Message}");
            }
            return users;
        }

        public async Task<bool> CreateUserAsync(User user)
        {
            try
            {

                await _firebaseClient
                    .Child("users")
                    .Child(user.Id)
                    .PutAsync(user);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка создания пользователя: {ex.Message}", "Error");
                Console.WriteLine($"CreateUser error: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> UpdateUserAvatarAsync(string userId, string avatarUrl)
        {
            try
            {
                Console.WriteLine($" [UpdateUserAvatarAsync] Setting avatar for user {userId}: {avatarUrl}");

                await _firebaseClient
                    .Child("users")
                    .Child(userId)
                    .Child("avatarUrl")
                    .PutAsync(avatarUrl ?? "");

                Console.WriteLine($"[UpdateUserAvatarAsync] Avatar updated successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateUserAvatarAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                if (!string.IsNullOrEmpty(user.AvatarUrl))
                {
                    Console.WriteLine($"[FirebaseService] Saving user with AvatarUrl: {user.AvatarUrl}");

                    if (user.AvatarUrl.StartsWith("http"))
                    {
                        Console.WriteLine($"Avatar is HTTP URL");
                    }
                    else if (File.Exists(user.AvatarUrl))
                    {
                        Console.WriteLine($" Avatar is local file: {user.AvatarUrl}");
                    }
                    else
                    {
                        Console.WriteLine($" Avatar path may be invalid: {user.AvatarUrl}");
                    }
                }
                else
                {
                    Console.WriteLine($" AvatarUrl is empty or null");
                }

                await _firebaseClient
                    .Child("users")
                    .Child(user.Id)
                    .PutAsync(user);

                Console.WriteLine($"✅ [FirebaseService] User updated successfully");

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка обновления пользователя: {ex.Message}", "Error");
                Console.WriteLine($"❌ UpdateUserAsync error: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        // Chat operations

        public async Task<Chat?> GetChatAsync(string chatId)
        {
            try
            {
                return await _firebaseClient
                    .Child("chats")
                    .Child(chatId)
                    .OnceSingleAsync<Chat>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetChat error: {ex.Message}");
                return null;
            }
        }

        public async Task<Chat?> FindExistingPrivateChatAsync(string userId1, string userId2)
        {
            try
            {
                MessageBox.Show($"Ищем существующий чат между {userId1} и {userId2}", "Отладка");

                var allChats = await _firebaseClient
                    .Child("chats")
                    .OnceAsync<Chat>();

                MessageBox.Show($"Всего чатов в базе: {allChats.Count()}", "Отладка");

                foreach (var chat in allChats)
                {
                    var participants = chat.Object.Participants ?? new List<string>();

                    bool hasUser1 = participants.Contains(userId1);
                    bool hasUser2 = participants.Contains(userId2);
                    bool isPrivate = chat.Object.Type == ChatType.Private;

                    if (isPrivate && hasUser1 && hasUser2)
                    {
                        MessageBox.Show($"✅ НАЙДЕН существующий чат!\n" +
                                       $"ID: {chat.Object.Id}\n" +
                                       $"Название: {chat.Object.Name}\n" +
                                       $"Участники: {string.Join(", ", participants)}",
                                       "Успех");
                        return chat.Object;
                    }
                }

                MessageBox.Show($"❌ Существующий чат не найден", "Отладка");
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска чата: {ex.Message}", "Ошибка");
                return null;
            }
        }

        public async Task<List<Chat>> GetUserChatsAsync(string userId)
        {
            var chats = new List<Chat>();
            try
            {

                var userChatsPath = $"userChats/{userId}";

                List<string> chatIds = null;

                try
                {
                    chatIds = await _firebaseClient
                        .Child(userChatsPath)
                        .OnceSingleAsync<List<string>>();

                }
                catch (Exception listEx)
                {


                    try
                    {
                        var chatDict = await _firebaseClient
                            .Child(userChatsPath)
                            .OnceSingleAsync<Dictionary<string, bool>>();

                        if (chatDict != null)
                        {
                            chatIds = chatDict.Keys.ToList();
                        }
                    }
                    catch (Exception dictEx)
                    {
                        MessageBox.Show($"Не удалось прочитать как Dictionary: {dictEx.Message}", "Debug");
                    }
                }

                if (chatIds != null && chatIds.Count > 0)
                {
                    foreach (var chatId in chatIds)
                    {
                        try
                        {
                            var chat = await GetChatAsync(chatId);
                            if (chat != null)
                            {
                                chats.Add(chat);
                                
                            }
                        }
                        catch (Exception ex)
                        {
                            
                        }
                    }
                }
                else
                { 

                    var allChats = await _firebaseClient
                        .Child("chats")
                        .OnceAsync<Chat>();

                    foreach (var chat in allChats)
                    {
                        if (chat.Object.Participants.Contains(userId))
                        {
                            chats.Add(chat.Object);

                            await AddUserToChatAsync(userId, chat.Object.Id);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка GetUserChatsAsync: {ex.Message}", "Error");
                Console.WriteLine($"GetUserChatsAsync error: {ex.Message}\n{ex.StackTrace}");
            }

            return chats;
        }



        public async Task DebugShowAllChats()
        {
            try
            {
                var allChats = await _firebaseClient
                    .Child("chats")
                    .OnceAsync<Chat>();

                var debugInfo = $"=== ВСЕ ЧАТЫ В БАЗЕ ===\n";
                debugInfo += $"Всего: {allChats.Count()}\n\n";

                foreach (var chat in allChats)
                {
                    debugInfo += $"💬 ЧАТ: {chat.Object.Name}\n";
                    debugInfo += $"   ID: {chat.Object.Id}\n";
                    debugInfo += $"   Тип: {chat.Object.Type}\n";
                    debugInfo += $"   Создал: {chat.Object.CreatedById}\n";
                    debugInfo += $"   Участники: {string.Join(", ", chat.Object.Participants ?? new List<string>())}\n";

                    if (chat.Object.LastMessage != null)
                    {
                        debugInfo += $"   Последнее сообщение: {chat.Object.LastMessage.Content}\n";
                    }
                    debugInfo += $"---\n";
                }

                MessageBox.Show(debugInfo, "Debug: Все чаты");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Error");
            }
        }

        public async Task<Chat> CreateChatAsync(Chat chat)
        {
            try
            {
                

                await _firebaseClient
                    .Child("chats")
                    .Child(chat.Id)
                    .PutAsync(chat);

                MessageBox.Show($"✅ Чат создан", "Debug");
                return chat;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка создания чата: {ex.Message}", "Error");
                Console.WriteLine($"CreateChat error: {ex.Message}");
                return chat;
            }
        }


        public async Task<List<Chat>> GetAllChatsAsync()
        {
            var chats = new List<Chat>();
            try
            {
                var allChats = await _firebaseClient
                    .Child("chats")
                    .OnceAsync<Chat>();

                foreach (var chat in allChats)
                {
                    chats.Add(chat.Object);
                }
            }
            catch { }
            return chats;
        }

        private async Task RemoveChatFromUserAsync(string userId, string chatId)
        {
            try
            {
                var userChatsPath = $"userChats/{userId}";
                var currentChats = await _firebaseClient
                    .Child(userChatsPath)
                    .OnceSingleAsync<List<string>>();

                if (currentChats != null && currentChats.Contains(chatId))
                {
                    currentChats.Remove(chatId);
                    await _firebaseClient
                        .Child(userChatsPath)
                        .PutAsync(currentChats);

                    MessageBox.Show($"✅ Удален чат {chatId} у пользователя {userId}", "Отладка");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления чата у пользователя: {ex.Message}", "Ошибка");
            }
        }


        public async Task DeleteChatAsync(string chatId)
        {
            try
            {
                MessageBox.Show($"Удаляю чат: {chatId}", "Отладка");

                await _firebaseClient
                    .Child("chats")
                    .Child(chatId)
                    .DeleteAsync();

                var chat = await GetChatAsync(chatId);
                if (chat != null && chat.Participants != null)
                {
                    foreach (var userId in chat.Participants)
                    {
                        await RemoveChatFromUserAsync(userId, chatId);
                    }
                }

                MessageBox.Show($"✅ Чат {chatId} удален", "Успех");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка удаления чата: {ex.Message}", "Ошибка");
                throw;
            }
        }

        // Message operations

        public async Task SendMessageAsync(Message message)
        {
            try
            {
                MessageBox.Show($"Отправляем сообщение:\n" +
                              $"Чат: {message.ChatId}\n" +
                              $"Отправитель: {message.SenderName} (ID: {message.SenderId})\n" +
                              $"Сообщение: {message.Content}", "Debug");

                await _firebaseClient
                    .Child("messages")
                    .Child(message.ChatId)
                    .Child(message.Id)
                    .PutAsync(message);

                await _firebaseClient
                    .Child("chats")
                    .Child(message.ChatId)
                    .Child("lastMessage")
                    .PutAsync(message);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка отправки сообщения: {ex.Message}", "Error");
                Console.WriteLine($"SendMessage error: {ex.Message}");
            }
        }

        public async Task<bool> UpdateMessageAsync(Message message)
        {
            try
            {
                if (string.IsNullOrEmpty(message.Id) || string.IsNullOrEmpty(message.ChatId))
                    return false;

                var messageData = JsonConvert.SerializeObject(message);
                await _firebaseClient
                    .Child("messages")
                    .Child(message.ChatId)
                    .Child(message.Id)
                    .PutAsync(messageData);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateMessageAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteMessageAsync(string messageId, string chatId)
        {
            try
            {
                if (string.IsNullOrEmpty(messageId) || string.IsNullOrEmpty(chatId))
                    return false;

                await _firebaseClient
                    .Child("messages")
                    .Child(chatId)
                    .Child(messageId)
                    .DeleteAsync();

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DeleteMessageAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Message>> GetChatMessagesAsync(string chatId, int limit = 50)
        {
            var messages = new List<Message>();
            try
            {


                var messageData = await _firebaseClient
                    .Child("messages")
                    .Child(chatId)
                    .OrderByKey()
                    .LimitToLast(limit)
                    .OnceAsync<Message>();

                foreach (var msg in messageData)
                {
                    messages.Add(msg.Object);
                    
                }

                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка загрузки сообщений: {ex.Message}", "Error");
                Console.WriteLine($"GetChatMessages error: {ex.Message}");
            }
            return messages;
        }

        public IObservable<FirebaseEvent<Message>> ObserveChatMessages(string chatId)
        {
            try
            {
                return _firebaseClient
                    .Child("messages")
                    .Child(chatId)
                    .AsObservable<Message>();
            }
            catch
            {
                return Observable.Empty<FirebaseEvent<Message>>();
            }
        }

        public IObservable<FirebaseEvent<Chat>> ObserveUserChats(string userId)
        {
            try
            {
                return _firebaseClient
                    .Child("chats")
                    .AsObservable<Chat>()
                    .Where(chat => chat.Object.Participants.Contains(userId));
            }
            catch
            {
                return System.Reactive.Linq.Observable.Empty<FirebaseEvent<Chat>>();
            }
        }

        public async Task FixUserChatsStructureAsync(string userId)
        {
            try
            {

                var userChatsPath = $"userChats/{userId}";

                try
                {
                    var asDict = await _firebaseClient
                        .Child(userChatsPath)
                        .OnceSingleAsync<Dictionary<string, object>>();

                    if (asDict != null)
                    {
                        var chatIds = asDict.Keys.ToList();
                        await _firebaseClient
                            .Child(userChatsPath)
                            .PutAsync(chatIds);
                        return;
                    }
                }
                catch { }

                try
                {
                    var asList = await _firebaseClient
                        .Child(userChatsPath)
                        .OnceSingleAsync<List<string>>();

                    if (asList != null)
                    {
                        return;
                    }
                }
                catch { }

                await _firebaseClient
                    .Child(userChatsPath)
                    .PutAsync(new List<string>());

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка исправления структуры: {ex.Message}", "Error");
            }
        }

        public async Task AddUserToChatAsync(string userId, string chatId)
        {
            try
            {

                var userChatsPath = $"userChats/{userId}";

                List<string> currentChats = null;

                try
                {
                    currentChats = await _firebaseClient
                        .Child(userChatsPath)
                        .OnceSingleAsync<List<string>>();
                }
                catch
                {
                    currentChats = new List<string>();
                }

                if (currentChats == null)
                {
                    currentChats = new List<string>();
                }

                if (!currentChats.Contains(chatId))
                {
                    currentChats.Add(chatId);

                    await _firebaseClient
                        .Child(userChatsPath)
                        .PutAsync(currentChats);
                    var savedChats = await _firebaseClient
                        .Child(userChatsPath)
                        .OnceSingleAsync<List<string>>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка добавления чата: {ex.Message}", "Error");
                Console.WriteLine($"AddUserToChat error: {ex.Message}\n{ex.StackTrace}");

                await AddUserToChatSimpleAsync(userId, chatId);
            }
        }

        private async Task AddUserToChatSimpleAsync(string userId, string chatId)
        {
            try
            {
                await _firebaseClient
                    .Child("userChats")
                    .Child(userId)
                    .PutAsync(new List<string> { chatId });

                MessageBox.Show($"✅ Чат добавлен (простой способ)", "Debug");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ И простой способ не сработал: {ex.Message}", "Critical");
            }
        }

        public async Task<List<string>> GetUserChatIdsAsync(string userId)
        {
            try
            {
                var chatIds = await _firebaseClient
                    .Child("userChats")
                    .Child(userId)
                    .OnceSingleAsync<List<string>>();

                return chatIds ?? new List<string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetUserChatIds error: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task UpdateChatParticipantsAsync(string chatId, List<string> participants)
        {
            try
            {
                await _firebaseClient
                    .Child("chats")
                    .Child(chatId)
                    .Child("participants")
                    .PutAsync(participants);

                Console.WriteLine($"✅ Участники чата {chatId} обновлены");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateChatParticipants error: {ex.Message}");
                throw;
            }
        }

        // Дополнительный метод для получения всех пользователей (для отладки)
        public async Task<List<User>> GetAllUsersAsync()
        {
            var users = new List<User>();
            try
            {
                var allUsers = await _firebaseClient
                    .Child("users")
                    .OnceAsync<User>();

                foreach (var user in allUsers)
                {
                    users.Add(user.Object);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAllUsers error: {ex.Message}");
            }
            return users;
        }

        // Метод для отладки: показать всех пользователей
        public async Task DebugShowAllUsers()
        {
            try
            {
                var allUsers = await GetAllUsersAsync();
                var debugInfo = $"=== ВСЕ ПОЛЬЗОВАТЕЛИ В БАЗЕ ===\n";
                debugInfo += $"Всего: {allUsers.Count}\n\n";

                foreach (var user in allUsers)
                {
                    debugInfo += $"👤 {user.Username}\n";
                    debugInfo += $"   ID: {user.Id}\n";
                    debugInfo += $"   UID: {user.Uid}\n";
                    debugInfo += $"   Email: {user.Email}\n";
                    debugInfo += $"   DisplayName: {user.DisplayName}\n";
                    debugInfo += $"---\n";
                }

                MessageBox.Show(debugInfo, "Debug: Все пользователи");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения пользователей: {ex.Message}", "Error");
            }
        }
    }
}