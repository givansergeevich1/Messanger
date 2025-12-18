using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace Messenger.Services
{
    public class FileService
    {
        // Папка для временных файлов
        private readonly string _tempFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Messenger",
            "Temp");

        // Папка для загруженных файлов
        private readonly string _downloadsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Messenger Downloads");

        public FileService()
        {
            // Создаём папки если их нет
            Directory.CreateDirectory(_tempFolder);
            Directory.CreateDirectory(_downloadsFolder);
        }

        // Выбор файла через диалог
        public (string path, string fileName, long size, string type)? SelectFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Выберите файл для отправки",
                Filter = "Все файлы (*.*)|*.*|" +
                        "Изображения (*.jpg;*.jpeg;*.png;*.gif)|*.jpg;*.jpeg;*.png;*.gif|" +
                        "Документы (*.pdf;*.doc;*.docx;*.txt)|*.pdf;*.doc;*.docx;*.txt|" +
                        "Аудио (*.mp3;*.wav)|*.mp3;*.wav|" +
                        "Видео (*.mp4;*.avi)|*.mp4;*.avi",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var fileInfo = new FileInfo(openFileDialog.FileName);
                var fileType = GetFileType(fileInfo.Extension);

                return (openFileDialog.FileName,
                        fileInfo.Name,
                        fileInfo.Length,
                        fileType);
            }

            return null;
        }

        // Определяем тип файла по расширению
        private string GetFileType(string extension)
        {
            return extension.ToLower() switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "image",
                ".pdf" or ".doc" or ".docx" or ".txt" or ".xlsx" => "document",
                ".mp3" or ".wav" or ".ogg" => "audio",
                ".mp4" or ".avi" or ".mov" => "video",
                _ => "file"
            };
        }

        // Копируем файл во временную папку
        public string CopyToTemp(string sourcePath)
        {
            var fileName = Path.GetFileName(sourcePath);
            var tempPath = Path.Combine(_tempFolder, Guid.NewGuid().ToString() + "_" + fileName);

            File.Copy(sourcePath, tempPath, true);
            return tempPath;
        }

        // Сохраняем файл в папку загрузок
        public string SaveToDownloads(string sourcePath, string fileName)
        {
            var downloadPath = Path.Combine(_downloadsFolder, fileName);

            // Если файл уже существует, добавляем число
            int counter = 1;
            while (File.Exists(downloadPath))
            {
                var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                var extension = Path.GetExtension(fileName);
                downloadPath = Path.Combine(_downloadsFolder,
                    $"{nameWithoutExt} ({counter}){extension}");
                counter++;
            }

            File.Copy(sourcePath, downloadPath, true);
            return downloadPath;
        }

        // Очистка временных файлов
        public void CleanTempFiles()
        {
            try
            {
                foreach (var file in Directory.GetFiles(_tempFolder))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch { }
                }
            }
            catch { }
        }

        // Получаем иконку для типа файла
        public string GetFileIcon(string fileType)
        {
            return fileType switch
            {
                "image" => "🖼️",
                "document" => "📄",
                "audio" => "🎵",
                "video" => "🎬",
                _ => "📎"
            };
        }

        // Форматируем размер файла
        public string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}