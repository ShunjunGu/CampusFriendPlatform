using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using CampusFriendPlatform.Data;
using CampusFriendPlatform.Models;

namespace CampusFriendPlatform.Controllers // 修正为项目实际使用的命名空间
{
    public partial class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult IntelligentAssistant()
        {
            ViewData["Title"] = "智能助手";
            return View();
        }

        // 定义一个 Session Key 来存储对话历史
        private const string ConversationHistorySessionKey = "_ConversationHistory";

        // Action to handle user prompt and get LLM response (POST)
        [HttpPost]
        public async Task<IActionResult> GetLlmResponse([FromBody] PromptRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Prompt))
            {
                return new BadRequestObjectResult(new { responseText = "无效的请求，请提供问题。" });
            }

            // --- Moonshot AI LLM 集成逻辑 (根据新的错误再次修正) ---

            // 1. 安全地获取 Moonshot AI API Key
            var moonshotApiKey = _configuration["MoonshotAI:ApiKey"];

            if (string.IsNullOrEmpty(moonshotApiKey))
            {
                _logger.LogError("Moonshot AI API Key is not configured.");
                return new ObjectResult(new { responseText = "AI 助手配置错误，请联系管理员。" }) { StatusCode = 500 };
            }

            // 2. 管理对话历史 (使用 HttpContext.Session 和 List<ChatMessage>)
            List<ChatMessage> messages = new List<ChatMessage>();

            var historyJson = HttpContext.Session.GetString(ConversationHistorySessionKey);
            if (!string.IsNullOrEmpty(historyJson))
            {
                try
                {
                    // 反序列化 JSON 字符串为 ChatMessage 对象列表
                    // System.Text.Json 反序列化复杂类型可能需要自定义 converter 或属性设置器
                    messages = System.Text.Json.JsonSerializer.Deserialize<List<ChatMessage>>(historyJson) ?? new List<ChatMessage>();
                }
                catch (System.Text.Json.JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize conversation history from session.");
                    HttpContext.Session.Remove(ConversationHistorySessionKey);
                    messages = new List<ChatMessage>(); // 从一个空列表开始
                }
            }

            // 如果历史记录为空或不包含系统消息，添加初始的 System Prompt
            if (!messages.Any() || !messages.Any(m => m.Role == CampusFriendPlatform.Models.ChatMessageRole.System))
            {
                messages.Insert(0, CampusFriendPlatform.Models.ChatMessage.CreateSystemMessage("你是 Kimi，由 Moonshot AI 提供的人工智能助手..."));
            }

            // 将当前用户的问题添加到消息列表中
            messages.Add(CampusFriendPlatform.Models.ChatMessage.CreateUserMessage(request.Prompt));

            // 3. 初始化 ChatClient
            // ChatClient 构造函数接收 model 和 apiKey
            var chatClient = new CampusFriendPlatform.Models.ChatClient("moonshot-v1-8k", moonshotApiKey);

            // 4. 调用 Moonshot AI API 获取对话完成
            try
            {
                // CompleteChatAsync 方法接收 List<ChatMessage>并返回字符串响应
                var llmResponseText = await chatClient.CompleteChatAsync(messages);

                // 5. 处理 API 调用结果
                if (!string.IsNullOrEmpty(llmResponseText))
                {
                    // 6. 将助手的回复消息添加到历史记录中并保存到 Session
                    messages.Add(ChatMessage.CreateAssistantMessage(llmResponseText));

                    // 将更新后的对话历史记录保存到 Session 中
                    HttpContext.Session.SetString(ConversationHistorySessionKey, System.Text.Json.JsonSerializer.Serialize(messages));

                    // 7. 将 LLM 的回复文本作为 JSON 数据返回给前端
                    return new OkObjectResult(new { responseText = llmResponseText });
                }
                else
                {
                    // 处理 API 调用失败或 LLM 没有返回有效回复的情况
                    _logger.LogError("Moonshot AI API call failed. No response text returned.");

                    // 返回错误信息给前端
                    return new ObjectResult(new { responseText = "AI 助手暂时无法回应，请稍后再试。" }) { StatusCode = 500 };
                }
            }
            catch (Exception ex)
            {
                // 捕获 API 调用或处理过程中的任何异常
                _logger.LogError(ex, "An error occurred while calling Moonshot AI API.");
                return new ObjectResult(new { responseText = "与 AI 助手通信时发生内部错误。" }) { StatusCode = 500 };
            }

            // --- 结束 Moonshot AI LLM 集成逻辑 ---
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // 示例用法（请根据实际需求调整）
        public void ExampleMethod()
        {
            var message = new ChatMessage("user", "Hello, world!");
            Console.WriteLine(message.Role); // 确保 Role 属性可用
        }
    }

// 定义一个简单模型来接收前端发送的数据
// 保留您原有的定义，并添加 default! 来解决 CS8618 警告
    public class PromptRequest
    {
        public string Prompt { get; set; } = string.Empty;
    }
}

// 注意：如果 JsonSerializer.Deserialize<List<ChatMessage>>(historyJson) 在运行时抛出异常，
// 很可能是因为 System.Text.Json 无法正确序列化/反序列化 ChatMessage 类。
// 您可能需要考虑以下方案之一：
// 1. 引入 Newtonsoft.Json (Json.Net) 并使用其序列化/反序列化方法。
//    需要在 Program.cs 中 AddControllers().AddNewtonsoftJson();
// 2. 创建一个简化的、可完全序列化的模型类用于 Session 存储，
//    并在 ChatMessage 和该模型之间进行手动映射（转换）。
// 3. 检查 ChatMessage 类是否有 JsonConstructor 属性或公共属性设置器，以帮助 System.Text.Json.

// Add Qwen integration classes inside namespace
namespace CampusFriendPlatform.Controllers
{
    public class QianWenRequest
    {
        public string Model { get; set; } = string.Empty;
        public Input Input { get; set; } = new Input();
    }

    public class Input
    {
        public string Prompt { get; set; } = string.Empty;
    }

    public class ChatRequest
    {
        public string Question { get; set; } = string.Empty;
    }

    // 扩展HomeController类，添加Chat方法
    public partial class HomeController
    {
        // Add Chat action method
        [HttpPost("/chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.Question))
                return new BadRequestObjectResult("Question cannot be empty");

            var client = new HttpClient();
            var requestUri = "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation";
            var apiKey = _configuration["Qwen:ApiKey"] ?? "YOUR_API_KEY";
            
            var requestObj = new QianWenRequest
            {
                Model = _configuration["Qwen:Model"] ?? "qwen-max",
                Input = new Input { Prompt = request.Question }
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(requestObj),
                Encoding.UTF8,
                "application/json"
            );

            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", apiKey);

            var response = await client.PostAsync(requestUri, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                return new OkObjectResult(new { response = responseBody });
            }
            
            return new ObjectResult("Error calling Qwen API") { StatusCode = (int)response.StatusCode };
        }
    }
}
