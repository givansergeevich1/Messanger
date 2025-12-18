
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace Messenger.Services
{
    public class LocalFileSharingService
    {
        private readonly ConcurrentDictionary<string, string> _fileMappings = new();

        private readonly string _sharedFilesFolder;

        public LocalFileSharingService()
        {
            _sharedFilesFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Messenger", "SharedFiles");

            Directory.CreateDirectory(_sharedFilesFolder);

            LoadMappings();
        }

        private void LoadMappings()
        {
            var mappingPath = Path.Combine(_sharedFilesFolder, "mappings.json");
            try
            {
                if (File.Exists(mappingPath))
                {
                    var json = File.ReadAllText(mappingPath);
                    var loaded = Newtonsoft.Json.JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>(json);
                    if (loaded != null)
                    {
                        foreach (var kvp in loaded)
                        {
                            _fileMappings[kvp.Key] = kvp.Value;
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"Loaded {_fileMappings.Count} mappings");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadMappings error: {ex.Message}");
            }
        }

        public async Task<string> ShareFileLocally(string filePath, string chatId, string fileName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== SHARE FILE LOCALLY ===");
                System.Diagnostics.Debug.WriteLine($"Original path: {filePath}");
                System.Diagnostics.Debug.WriteLine($"Chat ID: {chatId}");
                System.Diagnostics.Debug.WriteLine($"File name: {fileName}");

                var chatFolder = Path.Combine(_sharedFilesFolder, chatId);
                Directory.CreateDirectory(chatFolder);
                System.Diagnostics.Debug.WriteLine($"Chat folder: {chatFolder}");

                var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                var destinationPath = Path.Combine(chatFolder, uniqueFileName);
                File.Copy(filePath, destinationPath, true);
                System.Diagnostics.Debug.WriteLine($"Copied to: {destinationPath}");

                var fakeUrl = $"messenger-file://{chatId}/{uniqueFileName}";
                System.Diagnostics.Debug.WriteLine($"Fake URL: {fakeUrl}");

                _fileMappings[fakeUrl] = destinationPath;
                System.Diagnostics.Debug.WriteLine($"Mapping saved: {fakeUrl} -> {destinationPath}");

                await SaveMappingsAsync();

                return fakeUrl;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShareFileLocally ERROR: {ex}");
                MessageBox.Show($"Ошибка локального обмена: {ex.Message}", "Ошибка");
                return null;
            }
        }

        public string? GetLocalFilePath(string fakeUrl)
        {
            return _fileMappings.TryGetValue(fakeUrl, out var path) ? path : null;
        }

        private async Task SaveMappingsAsync()
        {
            var mappingPath = Path.Combine(_sharedFilesFolder, "mappings.json");
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(_fileMappings);
                await File.WriteAllTextAsync(mappingPath, json);
                System.Diagnostics.Debug.WriteLine($"Mappings saved to: {mappingPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveMappings error: {ex.Message}");
            }
        }
    }
}