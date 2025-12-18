using System;
using System.Threading.Tasks;
using Messenger.Models;
using System.Text.Json;
using System.Windows;

namespace Messenger.Services
{
    public class AuthService
    {
        private readonly FirebaseService _firebaseService;
        private readonly LocalStorageService _localStorage;
        private readonly FirebaseAuthService _firebaseAuth;
        private string? _currentIdToken;

        public AuthService(FirebaseService firebaseService,
                          LocalStorageService localStorage,
                          FirebaseAuthService firebaseAuth)
        {
            _firebaseService = firebaseService;
            _localStorage = localStorage;
            _firebaseAuth = firebaseAuth;
        }

        public async Task<User?> RegisterAsync(string username, string password, string email, string displayName)
        {
            try
            {
                
                var existingUser = await _firebaseService.GetUserByUsernameAsync(username);

                if (existingUser != null)
                {
                    MessageBox.Show($"Пользователь {username} уже существует в базе данных", "Debug");
                    Console.WriteLine($"User {username} already exists in database");
                    return null;
                }

                MessageBox.Show($"Firebase Auth регистрация...", "Debug");

                // Регистрируем в Firebase Authentication (REST API)
                var authResponse = await _firebaseAuth.RegisterAsync(email, password);

                if (authResponse == null)
                {
                    MessageBox.Show($"Firebase Auth регистрация FAILED!", "Debug Error");
                    return null;
                }

                _currentIdToken = authResponse.IdToken;


                var user = new User
                {
                    Id = authResponse.LocalId, 
                    Uid = authResponse.LocalId,
                    Username = username,
                    DisplayName = string.IsNullOrEmpty(displayName) ? username : displayName,
                    Email = email,
                    PasswordHash = "",
                    Status = UserStatus.Online,
                    LastSeen = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };


                var success = await _firebaseService.CreateUserAsync(user);
                if (!success)
                {
                    MessageBox.Show($"FAILED to save user to Firebase Database", "Debug Error");
                    return null;
                }

                User.CurrentUser = user;

                _localStorage.SaveUser(user);

                MessageBox.Show($"10. Регистрация УСПЕШНА! {username}\n" +
                              $"User ID: {user.Id}", "Final Success");
                
                return user;
            }
            catch (Exception ex) when (ex.Message.Contains("auth") || ex.Message.Contains("Auth"))
            {
                MessageBox.Show($"Firebase Authentication ERROR: {ex.Message}", "ERROR");
                Console.WriteLine($"Firebase Auth error: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"General ERROR: {ex.Message}\n{ex.StackTrace}", "ERROR");
                Console.WriteLine($"Registration error: {ex.Message}");
                return null;
            }
        }

        public async Task<User?> LoginAsync(string usernameOrEmail, string password)
        {
            try
            {

                string userEmail;

                if (usernameOrEmail.Contains("@"))
                {
                    userEmail = usernameOrEmail;
                }
                else
                {
                    var userFromDb = await _firebaseService.GetUserByUsernameAsync(usernameOrEmail);

                    if (userFromDb == null)
                    {
                        System.Windows.MessageBox.Show($"❌ Пользователь {usernameOrEmail} не найден в базе", "Debug");
                        return null;
                    }

                    userEmail = userFromDb.Email;
                }

                var authResponse = await _firebaseAuth.LoginAsync(userEmail, password);

                if (authResponse == null)
                {
                    System.Windows.MessageBox.Show($"❌ Firebase Auth не удался для {userEmail}", "Debug");
                    return null;
                }                

                User? user = await _firebaseService.GetUserByUidAsync(authResponse.LocalId);

                if (user == null)
                {
                    System.Windows.MessageBox.Show($"⚠️ Пользователь не найден по UID, ищем по email...", "Debug");
                    user = await _firebaseService.GetUserByEmailAsync(userEmail);

                    if (user == null)
                    {

                        user = new User
                        {
                            Id = authResponse.LocalId,
                            Username = usernameOrEmail.Contains("@") ? userEmail.Split('@')[0] : usernameOrEmail,
                            DisplayName = usernameOrEmail.Contains("@") ? userEmail.Split('@')[0] : usernameOrEmail,
                            Email = userEmail,
                            PasswordHash = "",
                            Status = UserStatus.Online,
                            LastSeen = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _firebaseService.CreateUserAsync(user);
                    }
                    else
                    {
                        user.Uid = authResponse.LocalId;

                        if (user.Id != authResponse.LocalId)
                        {
                            
                            user.Id = authResponse.LocalId;
                        }

                        await _firebaseService.UpdateUserAsync(user);
                    }
                }
               

                user.Status = UserStatus.Online;
                user.LastSeen = DateTime.UtcNow;
                await _firebaseService.UpdateUserAsync(user);

                User.CurrentUser = user;
                System.Windows.MessageBox.Show($"✅ User.CurrentUser установлен:\n" +
                                              $"ID: {User.CurrentUser.Id}\n" +
                                              $"Username: {User.CurrentUser.Username}\n" +
                                              $"Email: {User.CurrentUser.Email}", "Debug");

                _currentIdToken = authResponse.IdToken;

                _localStorage.SaveUser(user);

                System.Windows.MessageBox.Show($"✅ Вход успешен! Добро пожаловать, {user.DisplayName}!", "Debug");
                return user;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ Ошибка входа: {ex.Message}\n{ex.StackTrace}", "ERROR");
                return null;
            }
        }

        public void Logout()
        {
            try
            {
                System.Windows.MessageBox.Show("=== Logout начат ===", "Debug");

                if (User.CurrentUser != null)
                {
                    System.Windows.MessageBox.Show($"Выход для пользователя: {User.CurrentUser.Username}", "Debug");

                    var user = User.CurrentUser;
                    user.Status = UserStatus.Offline;
                    user.LastSeen = DateTime.UtcNow;

                    System.Windows.MessageBox.Show("Обновляем статус в Firebase...", "Debug");
                    Task.Run(async () =>
                    {
                        try
                        {
                            await _firebaseService.UpdateUserAsync(user);
                            System.Windows.MessageBox.Show("✅ Статус обновлен в Firebase", "Debug");
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show($"❌ Ошибка обновления статуса: {ex.Message}", "Debug");
                        }
                    });
                }
                else
                {
                    System.Windows.MessageBox.Show("User.CurrentUser уже null", "Debug");
                }

                System.Windows.MessageBox.Show("Выход из Firebase Auth...", "Debug");
                _firebaseAuth.Logout();

                _currentIdToken = null;

                System.Windows.MessageBox.Show("Очищаем User.CurrentUser...", "Debug");
                User.CurrentUser = null;

                System.Windows.MessageBox.Show("Очищаем локальное хранилище...", "Debug");
                _localStorage.ClearUser();

                System.Windows.MessageBox.Show("=== Logout завершен ===", "Debug");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ Ошибка выхода: {ex.Message}", "Error");
            }
        }

        public async Task<bool> ResetPasswordAsync(string email)
        {
            try
            {
                return await _firebaseAuth.ResetPasswordAsync(email);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Password reset error: {ex.Message}");
                return false;
            }
        }

        public async Task<User?> AutoLoginAsync()
        {
            try
            {
                var localUser = _localStorage.GetCurrentUser();

                if (localUser != null && !string.IsNullOrEmpty(localUser.Email))
                {
                    Console.WriteLine($"Found local user for auto-login: {localUser.Username}");

                    User? firebaseUser;

                    firebaseUser = await _firebaseService.GetUserByIdAsync(localUser.Id);

                    if (firebaseUser == null && !string.IsNullOrEmpty(localUser.Uid))
                    {
                        firebaseUser = await _firebaseService.GetUserByUidAsync(localUser.Uid);
                    }

                    if (firebaseUser != null)
                    {
                        User.CurrentUser = firebaseUser;

                        firebaseUser.Status = UserStatus.Online;
                        firebaseUser.LastSeen = DateTime.UtcNow;
                        await _firebaseService.UpdateUserAsync(firebaseUser);

                        Console.WriteLine($"Auto-login successful: {firebaseUser.Username}");
                        return firebaseUser;
                    }
                }

                Console.WriteLine("No local user found for auto-login");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Auto-login error: {ex.Message}");
                return null;
            }
        }

        public string? GetCurrentIdToken()
        {
            return _currentIdToken;
        }

        public bool IsUserLoggedIn()
        {
            return User.CurrentUser != null && !string.IsNullOrEmpty(_currentIdToken);
        }
    }
}