using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace CampusFriendPlatform.Models
{
    public class DeepSeekRequest
    {
        public string model { get; set; }
        public string prompt { get; set; }
        public int max_tokens { get; set; } = 2048;
        public float temperature { get; set; } = 1.0f;
        public float top_p { get; set; } = 1.0f;

        public DeepSeekRequest(string prompt, string model = "deepseek-chat")
        {
            this.prompt = prompt;
            this.model = model;
        }
    }

    public class DeepSeekResponse
    {
        public string? text { get; set; }
        public bool success { get; set; }
        public string? error { get; set; }
    }

    public class DeepSeekClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiUrl = "https://api.deepseek.com/v1/chat/completions";

        public DeepSeekClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["DeepSeek:ApiKey"] ?? throw new ArgumentException("DeepSeek API key not configured");
        }

        public async Task<DeepSeekResponse> SendPromptAsync(string prompt)
        {
            var request = new DeepSeekRequest(prompt);
            
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            httpRequest.Headers.Add("Content-Type", "application/json");
            
            var content = JsonConvert.SerializeObject(new
            {
                model = request.model,
                messages = new[] 
                {
                    new { role = "user", content = request.prompt }
                },
                max_tokens = request.max_tokens,
                temperature = request.temperature,
                top_p = request.top_p
            }, Formatting.None);
            
            httpRequest.Content = new StringContent(content, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.SendAsync(httpRequest);
            if (!response.IsSuccessStatusCode)
            {
                return new DeepSeekResponse 
                { 
                    success = false, 
                    error = $"HTTP error: {response.StatusCode}" 
                };
            }
            
            var responseContent = await response.Content.ReadAsStringAsync();
            dynamic responseObject = JsonConvert.DeserializeObject<dynamic>(responseContent);
            
            return new DeepSeekResponse
            {
                text = responseObject.choices[0].message.content,
                success = true
            };
        }
    }
}
