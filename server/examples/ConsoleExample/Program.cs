using Cube.Shared.Examples;
using Cube.Shared.Utils;
using System;
using System.Threading.Tasks;

namespace Cube.Examples.ConsoleApp;

/// <summary>
/// 配置和工具类使用示例程序
/// 演示如何使用我们创建的各种工具类
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Cube Server 工具类示例程序 ===");
        Console.WriteLine("请选择要运行的示例:");
        Console.WriteLine("1. 配置管理示例");
        Console.WriteLine("2. 日志系统示例"); 
        Console.WriteLine("3. 数据库连接示例");
        Console.WriteLine("4. 综合示例 (推荐)");
        Console.WriteLine("5. 退出");
        Console.WriteLine();

        while (true)
        {
            Console.Write("请输入选项 (1-5): ");
            var input = Console.ReadLine();
            
            switch (input)
            {
                case "1":
                    RunConfigurationExample();
                    break;
                case "2":
                    RunLogExample();
                    break;
                case "3":
                    await RunDatabaseExample();
                    break;
                case "4":
                    await RunComprehensiveExample();
                    break;
                case "5":
                    Console.WriteLine("再见!");
                    return;
                default:
                    Console.WriteLine("无效选项，请重新输入");
                    break;
            }
            
            Console.WriteLine("\n按任意键继续...");
            Console.ReadKey();
            ShowMenu();
        }
    }

    static void ShowMenu()
    {
        Console.Clear();
        Console.WriteLine("=== Cube Server 工具类示例程序 ===");
        Console.WriteLine("请选择要运行的示例:");
        Console.WriteLine("1. 配置管理示例");
        Console.WriteLine("2. 日志系统示例");
        Console.WriteLine("3. 数据库连接示例");
        Console.WriteLine("4. 综合示例 (推荐)");
        Console.WriteLine("5. 退出");
        Console.WriteLine();
    }

    /// <summary>
    /// 配置管理示例
    /// </summary>
    static void RunConfigurationExample()
    {
        Console.WriteLine("\n=== 配置管理示例 ===");
        
        try
        {
            ConfigExample.RunExample();
            Console.WriteLine("✅ 配置管理示例执行完成");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 配置示例执行失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 日志系统示例
    /// </summary>
    static void RunLogExample()
    {
        Console.WriteLine("\n=== 日志系统示例 ===");
        
        try
        {
            // 初始化日志系统
            LogUtils.Initialize(LogUtils.LogLevel.Debug, @"logs\example.log");
            
            // 不同级别的日志输出
            LogUtils.Debug("这是调试信息，参数值: {0}", 123);
            LogUtils.Info("应用程序启动成功");
            LogUtils.Warning("警告：内存使用率超过80%");
            
            try
            {
                // 模拟异常
                throw new InvalidOperationException("模拟业务异常");
            }
            catch (Exception ex)
            {
                LogUtils.Error("业务处理失败", ex);
            }

            LogUtils.Fatal("系统致命错误，请立即处理");

            Console.WriteLine($"日志文件路径: {LogUtils.GetLogFilePath()}");
            Console.WriteLine($"文件日志启用: {LogUtils.IsFileLoggingEnabled()}");
            Console.WriteLine("✅ 日志系统示例执行完成");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 日志示例执行失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 数据库连接示例
    /// </summary>
    static async Task RunDatabaseExample()
    {
        Console.WriteLine("\n=== 数据库连接示例 ===");
        Console.WriteLine("注意: 此示例需要运行中的MariaDB/MySQL数据库");
        
        try
        {
            // 初始化日志以便查看详细信息
            LogUtils.Initialize(LogUtils.LogLevel.Info);
            
            await ConfigExample.CreateDatabaseFromConfig().ExecuteInTransactionAsync(async (transaction) =>
            {
                // 这里只是演示连接，不执行实际操作
                LogUtils.Info("数据库连接测试成功");
                await Task.Delay(100); // 模拟操作
                return true;
            });
            
            Console.WriteLine("✅ 数据库连接示例执行完成");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  数据库示例执行失败 (这很正常，如果没有运行数据库): {ex.Message}");
            Console.WriteLine("💡 提示: 请确保MariaDB/MySQL正在运行，且配置正确");
        }
    }

    /// <summary>
    /// 综合示例
    /// </summary>
    static async Task RunComprehensiveExample()
    {
        Console.WriteLine("\n=== 综合示例 ===");
        Console.WriteLine("此示例将演示配置、日志、数据库的综合使用");
        
        try
        {
            await UtilsExamples.ComprehensiveExample();
            Console.WriteLine("✅ 综合示例执行完成");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 综合示例执行失败: {ex.Message}");
        }
    }
}