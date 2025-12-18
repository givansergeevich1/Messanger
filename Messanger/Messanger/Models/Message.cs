using System;
using System.IO;
using System.Reflection.Metadata;
using Newtonsoft.Json;

namespace Messenger.Models
{
    public enum MessageType
    {
        Text,
        Image,
        File
    }

    public enum MessageStatus
    {
        Sending,
        Sent,
        Delivered,
        Read,
        Failed
    }

    public class FileAttachment
    {
        [JsonProperty("url")]
        public string Url { get; set; } = string.Empty;

        [JsonProperty("fileName")]
        public string FileName { get; set; } = string.Empty;

        [JsonProperty("fileSize")]
        public long FileSize { get; set; }

        [JsonProperty("fileType")]
        public string FileType { get; set; } = string.Empty;

        [JsonProperty("localPath")]
        public string LocalPath { get; set; } = string.Empty;

        [JsonProperty("isUploaded")]
        public bool IsUploaded { get; set; }

        [JsonIgnore]
        public string FileSizeFormatted => FormatFileSize(FileSize);

        [JsonIgnore]
        public string FileIcon => GetFileIcon();

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        private string GetFileIcon()
        {
            var extension = Path.GetExtension(FileName)?.ToLower();

            return extension switch
            {
                ".pdf" => "📄",
                ".doc" or ".docx" => "📝",
                ".xls" or ".xlsx" => "📊",
                ".ppt" or ".pptx" => "📽️",
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "🖼️",
                ".zip" or ".rar" or ".7z" => "🗜️",
                ".mp3" or ".wav" or ".flac" => "🎵",
                ".mp4" or ".avi" or ".mkv" => "🎬",
                ".txt" => "📃",
                ".exe" => "⚙️",
                _ => "📎"
            };
        }
    }


    public class Message
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("chatId")]
        public string ChatId { get; set; } = string.Empty;

        [JsonProperty("senderId")]
        public string SenderId { get; set; } = string.Empty;

        [JsonProperty("senderName")]
        public string SenderName { get; set; } = string.Empty;

        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;

        [JsonProperty("messageType")]
        public MessageType MessageType { get; set; } = MessageType.Text;

        [JsonProperty("fileAttachment")]
        public FileAttachment FileAttachment { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonProperty("isEdited")]
        public bool IsEdited { get; set; }

        [JsonProperty("isDeleted")]
        public bool IsDeleted { get; set; }

        [JsonProperty("editedAt")]
        public DateTime? EditedAt { get; set; }

        [JsonProperty("deletedAt")]
        public DateTime? DeletedAt { get; set; }

        [JsonProperty("editedBy")]
        public string? EditedBy { get; set; }

        [JsonProperty("deletedBy")]
        public string? DeletedBy { get; set; }

        [JsonProperty("status")]
        public MessageStatus Status { get; set; } = MessageStatus.Sending;

        [JsonIgnore]
        public string DisplayTime => CreatedAt.ToLocalTime().ToString("HH:mm");

        [JsonIgnore]
        public string DisplayDate => CreatedAt.ToLocalTime().ToString("dd.MM.yyyy");

        [JsonIgnore]
        public bool HasAttachment => FileAttachment != null && !string.IsNullOrEmpty(FileAttachment.FileName);

        [JsonIgnore]
        public bool IsCurrentUser
        {
            get
            {
                try
                {
                    return !string.IsNullOrEmpty(SenderId) &&
                           User.CurrentUser != null &&
                           (SenderId == User.CurrentUser.Id ||
                            (!string.IsNullOrEmpty(User.CurrentUser.Uid) &&
                             SenderId == User.CurrentUser.Uid));
                }
                catch
                {
                    return false;
                }
            }
        }

        [JsonIgnore]
        public string FileUrl
        {
            get => FileAttachment?.Url ?? string.Empty;
            set
            {
                if (FileAttachment == null && !string.IsNullOrEmpty(value))
                {
                    FileAttachment = new FileAttachment { Url = value };
                }
                else if (FileAttachment != null)
                {
                    FileAttachment.Url = value;
                }
            }
        }

        [JsonIgnore]
        public string FileName
        {
            get => FileAttachment?.FileName ?? string.Empty;
            set
            {
                if (FileAttachment == null && !string.IsNullOrEmpty(value))
                {
                    FileAttachment = new FileAttachment { FileName = value };
                }
                else if (FileAttachment != null)
                {
                    FileAttachment.FileName = value;
                }
            }
        }

        [JsonIgnore]
        public long FileSize
        {
            get => FileAttachment?.FileSize ?? 0;
            set
            {
                if (FileAttachment == null && value > 0)
                {
                    FileAttachment = new FileAttachment { FileSize = value };
                }
                else if (FileAttachment != null)
                {
                    FileAttachment.FileSize = value;
                }
            }
        }

        // Конструктор для текстовых сообщений
        public Message() 
        {
            FileAttachment = null;
            IsEdited = false;
            IsDeleted = false;
            EditedAt = null;
            DeletedAt = null;
            EditedBy = null;
            DeletedBy = null;
        }

        // Конструктор для файловых сообщений
        public Message(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);

                FileAttachment = new FileAttachment
                {
                    FileName = fileInfo.Name,
                    FileSize = fileInfo.Length,
                    FileType = GetMimeType(fileInfo.Extension),
                    LocalPath = filePath
                };

                MessageType = IsImageFile(fileInfo.Extension) ? MessageType.Image : MessageType.File;
                Content = $"[Файл: {FileAttachment.FileName}]";
            }
        }

        private bool IsImageFile(string extension)
        {
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            return imageExtensions.Contains(extension.ToLower());
        }

        private string GetMimeType(string extension)
        {
            return extension.ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".zip" => "application/zip",
                ".mp3" => "audio/mpeg",
                ".mp4" => "video/mp4",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }
    }
}