using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Messenger.Models;

namespace Messenger.Services
{
    public class LocalStorageService
    {
        private const string SettingsFile = "settings.json";
        private const string UserFile = "user.json";

        private string GetAppDataPath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(appData, "Messenger");
        }

        private string GetFilePath(string fileName)
        {
            var appDataPath = GetAppDataPath();
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
            return Path.Combine(appDataPath, fileName);
        }

        public void SaveUser(User user)
        {
            try
            {
                var filePath = GetFilePath(UserFile);
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(user, options);
                File.WriteAllText(filePath, json);
                Console.WriteLine($"User saved to local storage: {user.Username}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving user to local storage: {ex.Message}");
            }
        }

        public User? GetCurrentUser()
        {
            var filePath = GetFilePath(UserFile);
            if (!File.Exists(filePath))
            {
                Console.WriteLine("No user file found in local storage");
                return null;
            }

            try
            {
                var json = File.ReadAllText(filePath);
                var user = JsonSerializer.Deserialize<User>(json);
                if (user != null)
                {
                    Console.WriteLine($"User loaded from local storage: {user.Username}");
                    User.CurrentUser = user;
                }
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading user from local storage: {ex.Message}");
                return null;
            }
        }

        public void ClearUser()
        {
            try
            {
                var filePath = GetFilePath(UserFile);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Console.WriteLine("User cleared from local storage");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing user from local storage: {ex.Message}");
            }
        }

        public bool IsUserLoggedIn()
        {
            var user = GetCurrentUser();
            return user != null;
        }

        public void SaveSetting(string key, string value)
        {
            try
            {
                var filePath = GetFilePath(SettingsFile);
                var settings = new Dictionary<string, string>();

                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                }

                settings[key] = value;
                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(filePath, JsonSerializer.Serialize(settings, options));
                Console.WriteLine($"Setting saved: {key} = {value}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving setting: {ex.Message}");
            }
        }

        public string? GetSetting(string key)
        {
            var filePath = GetFilePath(SettingsFile);
            if (!File.Exists(filePath))
                return null;

            try
            {
                var json = File.ReadAllText(filePath);
                var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                return settings?.GetValueOrDefault(key);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting setting: {ex.Message}");
                return null;
            }
        }
    }
}