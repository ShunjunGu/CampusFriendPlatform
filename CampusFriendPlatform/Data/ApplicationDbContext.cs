using Microsoft.EntityFrameworkCore;
using CampusFriendPlatform.Models; // 修正为项目实际使用的命名空间

namespace CampusFriendPlatform.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet 用于操作 Note 类型的数据库表
        public DbSet<Note> Notes { get; set; }
    }
}