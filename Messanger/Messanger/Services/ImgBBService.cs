using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace Messenger.Services
{
    public class ImgBBService
    {
        private readonly HttpClient _httpClient;
        private const string ApiKey = "85dcc71000215ed36cac1bda9ec74529"; 

        public ImgBBService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<string> UploadImageAsync(string imagePath)
        {
            try
            {
                var bytes = await File.ReadAllBytesAsync(imagePath);
                var base64Image = Convert.ToBase64String(bytes);

                var content = new MultipartFormDataContent();
                content.Add(new StringContent(base64Image), "image");

                var response = await _httpClient.PostAsync($"https://api.imgbb.com/1/upload?key={ApiKey}", content);
                var json = await response.Content.ReadAsStringAsync();

                var result = JObject.Parse(json);

                if (result["success"]?.Value<bool>() == true)
                {
                    return result["data"]?["url"]?.ToString();
                }

                MessageBox.Show($"ImgBB error: {result["error"]?["message"]}", "Ошибка");
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}", "Ошибка");
                return null;
            }
        }
    }
}