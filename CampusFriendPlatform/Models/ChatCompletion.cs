using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CampusFriendPlatform.Models
{
    public class ChatCompletion
    {
        [JsonPropertyName("choices")]
        public List<ChatCompletionChoice> Choices { get; set; } = new List<ChatCompletionChoice>();
    }

    public class ChatCompletionChoice
    {
        [JsonPropertyName("message")]
        public ChatCompletionMessage Message { get; set; } = new ChatCompletionMessage();
    }

    public class ChatCompletionMessage
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }
}