using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using CampusFriendPlatform.Models;

public class AiController : Controller
{
    private readonly DeepSeekClient _deepSeekClient;
    private readonly QwenClient _qwenClient;

    public AiController(DeepSeekClient deepSeekClient, QwenClient qwenClient)
    {
        _deepSeekClient = deepSeekClient;
        _qwenClient = qwenClient;
    }

    [HttpGet("/ai/ask")]
    public async Task<IActionResult> Ask(string prompt, string model = "deepseek")
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return BadRequest("Prompt cannot be empty");
        }

        try
        {
            if (model.ToLower() == "qwen")
            {
                var qwenResponse = await _qwenClient.SendPromptAsync(prompt);
                if (qwenResponse.success)
                {
                    return Content(qwenResponse.text);
                }
                return StatusCode(500, qwenResponse.error);
            }
            else
            {
                var deepSeekResponse = await _deepSeekClient.SendPromptAsync(prompt);
                if (deepSeekResponse.success)
                {
                    return Content(deepSeekResponse.text);
                }
                return StatusCode(500, deepSeekResponse.error);
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error processing request: {ex.Message}");
        }
    }
}