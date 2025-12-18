using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace Messenger.Services
{
    public class FirebaseAuthService
    {
        private const string ApiKey = "AIzaSyCJV46QpsMEV95POq354EhB9kg96BYB6r8";
        private const string SignUpUrl = "https://identitytoolkit.googleapis.com/v1/accounts:signUp";
        private const string SignInUrl = "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword";
        private const string ResetPasswordUrl = "https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode";

        private readonly HttpClient _httpClient;

        public FirebaseAuthService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<FirebaseAuthResponse?> RegisterAsync(string email, string password)
        {
            try
            {

                var requestData = new
                 {
                     email = email,
                     password = password,
                     returnSecureToken = true
                 };

                var json = JsonSerializer.Serialize(requestData);

                 var content = new StringContent(json, Encoding.UTF8, "application/json");

                 var url = $"{SignUpUrl}?key={ApiKey}";

                 
                 var response = await _httpClient.PostAsync(url, content);


                 var responseJson = await response.Content.ReadAsStringAsync();

                 response.EnsureSuccessStatusCode();

                 var authResponse = JsonSerializer.Deserialize<FirebaseAuthResponse>(responseJson);

                 return authResponse;
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"HTTP ОШИБКА: {ex.Message}\nStatus: {ex.StatusCode}", "ERROR");
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ОБЩАЯ ОШИБКА: {ex.Message}\nStack: {ex.StackTrace}", "ERROR");
                return null;
            }
        }

        public async Task<FirebaseAuthResponse?> LoginAsync(string email, string password)
        {
            try
            {

                var requestData = new
                {
                    email = email,
                    password = password,
                    returnSecureToken = true
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={ApiKey}";

                var response = await _httpClient.PostAsync(url, content);

                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var authResponse = JsonSerializer.Deserialize<FirebaseAuthResponse>(responseJson);
                return authResponse;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<bool> ResetPasswordAsync(string email)
        {
            try
            {
                var requestData = new
                {
                    requestType = "PASSWORD_RESET",
                    email = email
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{ResetPasswordUrl}?key={ApiKey}", content);
                response.EnsureSuccessStatusCode();

                Console.WriteLine($"Password reset email sent to: {email}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Password reset error: {ex.Message}");
                return false;
            }
        }

        public void Logout()
        {
            // При использовании REST API, "выход" - это просто удаление токена на клиенте
            Console.WriteLine("User logged out (token cleared locally)");
        }
    }

    public class FirebaseAuthResponse
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; } = string.Empty;

        [JsonPropertyName("localId")]
        public string LocalId { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("idToken")]
        public string IdToken { get; set; } = string.Empty;

        [JsonPropertyName("registered")]
        public bool Registered { get; set; }

        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("expiresIn")]
        public string ExpiresIn { get; set; } = string.Empty;
    }
}