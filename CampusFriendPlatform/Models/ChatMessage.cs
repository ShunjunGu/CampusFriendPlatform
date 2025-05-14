using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CampusFriendPlatform.Models // 修正为项目实际使用的命名空间
{
    public class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        public ChatMessage()
        {
        }

        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }

        public static ChatMessage CreateSystemMessage(string content)
        {
            return new ChatMessage(ChatMessageRole.System, content);
        }

        public static ChatMessage CreateUserMessage(string content)
        {
            return new ChatMessage(ChatMessageRole.User, content);
        }

        public static ChatMessage CreateAssistantMessage(string content)
        {
            return new ChatMessage(ChatMessageRole.Assistant, content);
        }
    }

    public static class ChatMessageRole
    {
        public const string System = "system";
        public const string User = "user";
        public const string Assistant = "assistant";
    }


}