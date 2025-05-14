using System;
using System.ComponentModel.DataAnnotations; // 添加这个 using 语句

namespace CampusFriendPlatform.Models // 修正为项目实际使用的命名空间
{
    public class Note
    {
        public int Id { get; set; } // 纸条的唯一标识符，EF Core 会自动设置为主键

        [Required(ErrorMessage = "请填写姓名")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "请填写年级")]
        public required string Grade { get; set; }

        [Required(ErrorMessage = "请选择性别")]
        public required string Gender { get; set; }

        [Phone(ErrorMessage = "请输入有效的手机号")]
        public required string PhoneNumber { get; set; }

        // 您可以根据需要添加其他属性，例如发布时间等
        // public DateTime CreatedDate { get; set; }
    }
}