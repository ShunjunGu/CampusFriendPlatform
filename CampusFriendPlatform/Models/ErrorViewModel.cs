namespace CampusFriendPlatform.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public string? ErrorMessage { get; set; } // 添加 ErrorMessage 属性
    }
}
