using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace CampusFriendPlatform.Models
{
    public class PromptRequest
    {
        public string model { get; set; }
        public Input input { get; set; }

        public PromptRequest(string prompt) : this(prompt, "qwen-max") { }

        public PromptRequest(string prompt, string model)
        {
            this.model = model;
            this.input = new Input(prompt);
        }
    }

    public class Input
    {
        public string prompt { get; set; }

        public Input(string prompt)
        {
            this.prompt = prompt;
        }
    }

    public class QwenResponse
    {
        public string? text { get; set; }
        public bool success { get; set; }
        public string? error { get; set; }
    }

    public class QwenClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _modelURL;

        public QwenClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Qwen:ApiKey"] ?? throw new ArgumentException("Qwen API key not configured");
            _modelURL = "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation";
        }

        public async Task<QwenResponse> SendPromptAsync(string prompt)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _modelURL);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                StringEscapeHandling = StringEscapeHandling.EscapeNonAscii
            };
            
            var message = new PromptRequest(prompt);
            string requestStr = JsonConvert.SerializeObject(message, settings);
            
            request.Content = new StringContent(requestStr, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var responseMessage = await response.Content.ReadAsStringAsync();
                dynamic responseAnswer = JsonConvert.DeserializeObject<dynamic>(responseMessage);
                
                return new QwenResponse
                {
                    text = responseAnswer.output.text,
                    success = true
                };
            }
            else
            {
                return new QwenResponse
                {
                    success = false,
                    error = $"HTTP error: {response.StatusCode}"
                };
            }
        }
    }
}