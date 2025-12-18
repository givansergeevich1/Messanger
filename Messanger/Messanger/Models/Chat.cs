using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Messenger.Models
{
    public enum ChatType
    {
        Private,
        Group
    }

    public class Chat
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("type")]
        public ChatType Type { get; set; } = ChatType.Private;

        [JsonProperty("createdById")]
        public string CreatedById { get; set; } = string.Empty;

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonProperty("avatarUrl")]
        public string AvatarUrl { get; set; } = string.Empty;

        [JsonProperty("participants")]
        public List<string> Participants { get; set; } = new List<string>();

        [JsonProperty("lastMessage")]
        public Message? LastMessage { get; set; }

        // Добавьте эти свойства для статусов
        [JsonIgnore]
        public UserStatus PartnerStatus { get; set; }

        [JsonIgnore]
        public DateTime PartnerLastSeen { get; set; }

        [JsonIgnore]
        public string DisplayName
        {
            get
            {
                return Name;
            }
        }

        [JsonIgnore]
        public string StatusText
        {
            get
            {
                if (PartnerStatus == UserStatus.Online)
                    return "🟢 Онлайн";

                if (PartnerStatus == UserStatus.DoNotDisturb)
                    return "⛔ Не беспокоить";

                // Оффлайн - показываем когда был в сети
                var timeAgo = DateTime.UtcNow - PartnerLastSeen;

                if (timeAgo.TotalMinutes < 1)
                    return "⚫ Был(а) только что";

                if (timeAgo.TotalHours < 1)
                    return $"⚫ Был(а) {(int)timeAgo.TotalMinutes} мин. назад";

                if (timeAgo.TotalDays < 1)
                    return $"⚫ Был(а) {(int)timeAgo.TotalHours} ч. назад";

                return $"⚫ Был(а) {(int)timeAgo.TotalDays} д. назад";
            }
        }

        [JsonIgnore]
        public string LastMessagePreview
        {
            get
            {
                if (LastMessage == null) return "Нет сообщений";

                // Обрезаем длинные сообщения
                if (LastMessage.Content.Length > 30)
                    return LastMessage.Content.Substring(0, 30) + "...";

                return LastMessage.Content;
            }
        }

        [JsonIgnore]
        public string LastMessageTime
        {
            get
            {
                if (LastMessage == null) return "";

                var now = DateTime.Now;
                var messageTime = LastMessage.CreatedAt.ToLocalTime();

                // Сегодня
                if (messageTime.Date == now.Date)
                    return messageTime.ToString("HH:mm");

                // Вчера
                if (messageTime.Date == now.Date.AddDays(-1))
                    return "Вчера";

                // На этой неделе
                if (messageTime.Date > now.Date.AddDays(-7))
                    return messageTime.ToString("dddd");

                // Старее недели
                return messageTime.ToString("dd.MM");
            }
        }

        [JsonIgnore]
        public int UnreadCount { get; set; } = 0;
    }
}