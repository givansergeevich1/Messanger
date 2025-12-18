using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Messenger.Services
{
    public class SimpleFileDownloader
    {
        public async Task<string> DownloadFileAsync(string url, string fileName)
        {
            try
            {
                var downloadsFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "Messenger Downloads");

                Directory.CreateDirectory(downloadsFolder);

                var savePath = GetUniqueFilePath(downloadsFolder, fileName);

                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromMinutes(2);

                    var bytes = await httpClient.GetByteArrayAsync(url);

                    await File.WriteAllBytesAsync(savePath, bytes);
                }

                return savePath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка скачивания файла: {ex.Message}");
            }
        }

        private string GetUniqueFilePath(string folder, string fileName)
        {
            var filePath = Path.Combine(folder, fileName);

            if (!File.Exists(filePath))
                return filePath;

            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var counter = 1;

            do
            {
                var newFileName = $"{nameWithoutExtension} ({counter}){extension}";
                filePath = Path.Combine(folder, newFileName);
                counter++;
            } while (File.Exists(filePath));

            return filePath;
        }
    }
}