using Microsoft.AspNetCore.Mvc;
using CampusFriendPlatform.Data; // 修正为项目实际使用的命名空间
using CampusFriendPlatform.Models; // 修正为项目实际使用的命名空间
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace CampusFriendPlatform.Controllers // 修正为项目实际使用的命名空间
{
    public class NotesController : Controller
    {
        private readonly ApplicationDbContext _context; // 用于数据库操作
        private readonly HttpClient _httpClient; // 用于调用 Kimi API
        private readonly Dictionary<string, List<Dictionary<string, string>>> _chatHistories = new();
        private const string KimiApiKey = "YOUR_KIMI_API_KEY"; // 替换为你的 Kimi API Key
        private const string KimiApiUrl = "https://api.moonshot.cn/v1/chat/completions";

        // 通过构造函数注入数据库上下文
        public NotesController(ApplicationDbContext context)
        {
            _context = context;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {KimiApiKey}");
        }

        // GET: /Notes/Create - 显示“放纸条”表单页面
        public IActionResult Create()
        {
            // 返回用于创建纸条的视图
            return View();
        }

        // POST: /Notes/Create - 处理“放纸条”表单提交
        [HttpPost]
        [ValidateAntiForgeryToken] // 防止跨站请求伪造
        public async Task<IActionResult> Create([Bind("Name,Grade,Gender,PhoneNumber")] Note note)
        {
            // 检查模型状态是否有效（基于模型类中的数据注解）
            if (ModelState.IsValid)
            {
                // 设置一些额外的数据，例如创建时间（如果您在模型中添加了该属性）
                // note.CreatedDate = DateTime.Now;

                // 将新的纸条对象添加到数据库上下文中
                _context.Add(note);

                // 异步保存更改到数据库
                await _context.SaveChangesAsync();

                // 重定向到主页或一个成功提示页面
                // 这里我们重定向到主页
                return RedirectToAction("Index", "Home");
            }

            // 如果模型验证失败，返回当前的视图，以便显示验证错误信息
            return View(note);
        }

        // GET: /Notes/Find - 显示“取纸条”表条页面
        public IActionResult Find()
        {
            // 返回用于查找纸条的视图
            return View();
        }

        // POST: /Notes/Find - 处理单条抽取
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Find([FromForm] Note searchCriteria)
        {
            // 仅验证年级和性别字段
            if (string.IsNullOrEmpty(searchCriteria.Grade) || 
                string.IsNullOrEmpty(searchCriteria.Gender))
            {
                ModelState.AddModelError("", "请填写年级和选择性别进行匹配。");
                return View(searchCriteria);
            }

            // 手动清除其他字段的验证错误
            ModelState.Remove(nameof(searchCriteria.Name));
            ModelState.Remove(nameof(searchCriteria.PhoneNumber));
            
            // 添加查询日志
            Console.WriteLine($"开始查询 - 年级: {searchCriteria.Grade}, 性别: {searchCriteria.Gender}");
            
            // 使用更可靠的随机查询方法
            var query = _context.Notes
                .Where(n => n.Grade == searchCriteria.Grade && 
                           n.Gender == searchCriteria.Gender);

            var recordCount = await query.CountAsync();
            
            if (recordCount > 0)
            {
                var randomIndex = new Random().Next(0, recordCount);
                var matchingNotes = await query
                    .Skip(randomIndex)
                    .Take(1)
                    .ToListAsync();

                // 添加结果日志
                Console.WriteLine($"找到 {matchingNotes.Count} 条匹配记录，随机索引: {randomIndex}");
                ViewBag.MatchFound = true;
                ViewBag.MatchedNotes = matchingNotes;
            }
            else
            {
                Console.WriteLine("未找到匹配记录");
                ViewBag.MatchFound = false;
                ViewBag.MatchedNotes = new List<Note>();
            }

            return View(searchCriteria);
        }

        // POST: /Notes/TenPacksDraw - 处理十连抽
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TenPacksDraw([FromForm] Note searchCriteria)
        {
            // 使用更可靠的随机查询方法
            var query = _context.Notes.AsQueryable();
            
            // 添加查询条件（如果有）
            if (!string.IsNullOrEmpty(searchCriteria.Grade) && !string.IsNullOrEmpty(searchCriteria.Gender))
            {
                query = query.Where(n => n.Grade == searchCriteria.Grade && n.Gender == searchCriteria.Gender);
            }
            
            // 记录查询条件
            Console.WriteLine($"开始随机查询 - 条件: {searchCriteria.Grade}/{searchCriteria.Gender}");
            
            // 获取总记录数
            var recordCount = await query.CountAsync();
            Console.WriteLine($"匹配记录总数: {recordCount}");
            
            // 生成随机起始点
            var random = new Random();
            var randomIndex = recordCount > 0 ? random.Next(0, recordCount) : 0;
            
            // 获取随机记录（最多取10条或剩余数量）
            var selectedNotes = await query
                .Skip(randomIndex)
                .Take(10)
                .ToListAsync();
            
            // 记录查询结果
            Console.WriteLine($"返回记录数: {selectedNotes.Count}, 随机起始点: {randomIndex}");
            
            ViewBag.MatchFound = selectedNotes.Count > 0;
            ViewBag.MatchedNotes = selectedNotes;
            
            return View("Find", searchCriteria);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetAiSuggestion([FromBody] AiRequest request)
        {
            if (string.IsNullOrEmpty(request?.UserInput))
            {
                return new BadRequestObjectResult(new { error = "无效的输入" });
            }

            // 获取或创建当前用户的对话历史
            var userKey = Request.HttpContext.Connection.Id;
            if (!_chatHistories.TryGetValue(userKey, out var messages))
            {
                messages = new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string>
                    {
                        { "role", "system" },
                        { "content", "你是 Kimi，由 Moonshot AI 提供的人工智能助手，你更擅长中文和英文的对话。你会为用户提供安全，有帮助，准确的回答。同时，你会拒绝一切涉及恐怖主义，种族歧视，黄色暴力等问题的回答。Moonshot AI 为专有名词，不可翻译成其他语言。" }
                    }
                };
                _chatHistories[userKey] = messages;
            }

            // 添加用户输入到消息列表
            messages.Add(new Dictionary<string, string>
            {
                { "role", "user" },
                { "content", request.UserInput }
            });

            var requestBody = new
            {
                model = "moonshot-v1-8k",
                messages = messages,
                temperature = 0.3f
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(KimiApiUrl, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var responseObject = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseString);
                    
                    if (responseObject.TryGetProperty("choices", out var choicesArray) && 
                        choicesArray.GetArrayLength() > 0 &&
                        choicesArray[0].TryGetProperty("message", out var messageObject) &&
                        messageObject.TryGetProperty("content", out var contentElement))
                    {
                        var assistantResponse = contentElement.GetString();
                        
                        // 添加 Kimi 回应到对话历史
                        messages.Add(new Dictionary<string, string>
                        {
                            { "role", "assistant" },
                            { "content", assistantResponse ?? string.Empty }
                        });
                        
                        return new OkObjectResult(new { response = assistantResponse });
                    }
                    
                    return new BadRequestObjectResult(new { error = "无法解析 API 响应内容。" });
                }
                else
                {
                    return new BadRequestObjectResult(new { error = $"API 请求失败，状态码：{response.StatusCode}" });
                }
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { error = $"调用 Kimi API 时发生错误：{ex.Message}" });
            }
        }

        // 可能会有一个 Action 来显示匹配结果，或者在 Find Action 中处理
        // public IActionResult MatchResult(Note match)
        // {
        //     return View(match);
        // }
    }
}