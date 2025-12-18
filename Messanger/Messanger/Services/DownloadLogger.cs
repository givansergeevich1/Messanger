
using System.IO;

namespace Messenger.ViewModels
{
    public class DownloadLogger
    {
        private readonly string _logPath;

        public DownloadLogger()
        {
            _logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Messenger",
                "downloads.log");

            Directory.CreateDirectory(Path.GetDirectoryName(_logPath));
        }

        public void LogDownload(string fileName, string url, string savePath, bool success, string error = "")
        {
            try
            {
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                              $"Файл: {fileName}, " +
                              $"URL: {url}, " +
                              $"Путь: {savePath}, " +
                              $"Успех: {success}, " +
                              $"Ошибка: {error}\n";

                File.AppendAllText(_logPath, logEntry);
            }
            catch
            {
                // Игнорируем ошибки логирования
            }
        }
    }
}