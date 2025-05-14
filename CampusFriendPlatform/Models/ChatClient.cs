using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CampusFriendPlatform.Models
{
    public class ChatClient
    {
        private readonly string _model;
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private readonly string _apiEndpoint = "https://api.moonshot.cn/v1/chat/completions";

        public ChatClient(string model, string apiKey)
        {
            _model = model;
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<string> CompleteChatAsync(List<ChatMessage> messages)
        {
            var requestBody = new
            {
                model = _model,
                messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToList(),
                temperature = 0.3f
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(_apiEndpoint, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<JsonElement>(responseString);

            if (responseObject.TryGetProperty("choices", out var choicesArray) &&
                choicesArray.GetArrayLength() > 0 &&
                choicesArray[0].TryGetProperty("message", out var messageObject) &&
                messageObject.TryGetProperty("content", out var contentElement))
            {
                return contentElement.GetString() ?? string.Empty;
            }

            throw new Exception("无法解析API响应内容");
        }
    }
}