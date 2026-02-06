using Cube.Shared.Utils;

namespace Cube.Test;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Cube Server 工具类快速测试 ===");
        
        // 测试配置加载
        Console.WriteLine("\n1. 测试配置加载...");
        try
        {
            SimpleConfig.LoadEnv();
            SimpleConfig.Initialize("Development");
            var dbHost = SimpleConfig.GetString("Database:Host", "localhost");
            Console.WriteLine($"✅ 配置加载成功 - 数据库主机: {dbHost}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 配置加载失败: {ex.Message}");
        }

        // 测试日志系统
        Console.WriteLine("\n2. 测试日志系统...");
        try
        {
            LogUtils.Initialize(LogUtils.LogLevel.Info, @"logs\test.log");
            LogUtils.Info("日志系统测试消息");
            Console.WriteLine("✅ 日志系统工作正常");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 日志系统测试失败: {ex.Message}");
        }

        // 测试数据库连接（如果可能）
        Console.WriteLine("\n3. 测试数据库连接...");
        try
        {
            var db = Database.Create(
                SimpleConfig.GetString("DATABASE_HOST") ?? "localhost",
                SimpleConfig.GetString("DATABASE_NAME") ?? "cube_game",
                SimpleConfig.GetString("DATABASE_USER") ?? "cube_user",
                SimpleConfig.GetString("DATABASE_PASSWORD") ?? "cube_password"
            );
            
            await db.OpenAsync();
            Console.WriteLine("✅ 数据库连接成功");
            await db.CloseAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  数据库连接测试: {ex.Message}");
            Console.WriteLine("   (这很正常，如果没有运行数据库)");
        }

        Console.WriteLine("\n=== 测试完成 ===");
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }
}