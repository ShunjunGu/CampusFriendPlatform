using Microsoft.EntityFrameworkCore;
using CampusFriendPlatform.Data; // 引用数据库上下文的命名空间
using CampusFriendPlatform.Models; // 添加缺失的命名空间引用

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 注册数据库上下文
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// 注册Session服务
builder.Services.AddSession(options =>
{
    // 从配置文件读取Session超时时间并进行null验证
    var timeoutMinutes = builder.Configuration.GetValue<int?>("Session:TimeoutMinutes");
    options.IdleTimeout = TimeSpan.FromMinutes(timeoutMinutes ?? 30); // 默认30分钟
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 注册QwenClient服务
builder.Services.AddHttpClient();
builder.Services.AddSingleton<QwenClient>(sp => 
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient();
    var configuration = sp.GetRequiredService<IConfiguration>();
    return new QwenClient(httpClient, configuration);
});

// 注册DeepSeekClient服务
builder.Services.AddSingleton<DeepSeekClient>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient();
    var configuration = sp.GetRequiredService<IConfiguration>();
    return new DeepSeekClient(httpClient, configuration);
});

var app = builder.Build();

// 启用静态资源映射
// app.MapStaticAssets(); // Commenting out this call as its origin/necessity is unclear and might be related to the error. Standard static file serving is usually handled by UseStaticFiles().

// 配置 HTTP 请求处理管道。

// 在非开发环境下配置错误处理和 HSTS - 保留您原有的代码
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// 强制将 HTTP 请求重定向到 HTTPS - 保留您原有的代码
app.UseHttpsRedirection();
app.UseStaticFiles(); // 启用静态文件中间件，确保能够正确加载静态资源


// 启用路由功能 - 保留您原有的代码
app.UseRouting();

// 启用 Session 中间件 - **新增**
// Session 中间件必须放在 UseRouting 之后，以及任何需要访问 Session 的中间件（如 UseAuthorization）之前
app.UseSession();

// 启用授权中间件 - 保留您原有的代码
app.UseAuthorization(); // 如果您的应用使用授权，确保它在 UseSession 之后

// 保留您的自定义静态资产映射和路由配置 - 保留您原有的代码
// app.MapStaticAssets(); // Removed duplicate or problematic call

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
    // .WithStaticAssets(); // Removed problematic call


// 运行应用程序 - 保留您原有的代码
app.Run();

// 如果您使用了顶级语句 (Top-level statements)，需要定义一个 partial class 来支持 AddUserSecrets<T> - **新增**
public partial class Program { }