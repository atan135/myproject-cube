using Cube.Shared.Utils;

namespace Cube.Shared.Examples;

/// <summary>
/// 工具类使用示例
/// 展示如何使用LogUtils和Database类
/// </summary>
public static class UtilsExamples
{
    /// <summary>
    /// 日志工具使用示例
    /// </summary>
    public static void LogUtilsExample()
    {
        // 初始化日志系统
        LogUtils.Initialize(
            LogUtils.LogLevel.Debug, 
            @"logs\app.log"
        );

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
    }

    /// <summary>
    /// 数据库工具使用示例
    /// </summary>
    public static async Task DatabaseExample()
    {
        LogUtils.Info("=== 数据库连接池示例 ===");
        
        // 方式1：使用连接池（默认）
        var pooledDb = Database.Create(
            host: "localhost",
            database: "cube_game",
            username: "root",
            password: "password"
        );
        
        // 方式2：不使用连接池
        var unpooledDb = Database.CreateWithoutPooling(
            host: "localhost",
            database: "cube_game",
            username: "root",
            password: "password"
        );

        try
        {
            // 使用连接池的数据库操作
            await TestDatabaseOperations(pooledDb, "连接池模式");
            
            // 不使用连接池的数据库操作
            await TestDatabaseOperations(unpooledDb, "无连接池模式");
            
        }
        catch (Exception ex)
        {
            LogUtils.Error("数据库操作失败", ex);
        }
        finally
        {
            // 关闭连接
            await pooledDb.DisposeAsync();
            await unpooledDb.DisposeAsync();
        }
    }
    
    /// <summary>
    /// 测试数据库操作
    /// </summary>
    private static async Task TestDatabaseOperations(Database db, string mode)
    {
        LogUtils.Info($"开始测试 {mode}");
        
        try
        {
            // 打开连接
            await db.OpenAsync();

            // 检查表是否存在
            bool userTableExists = await db.TableExistsAsync("users");
            LogUtils.Info($"{mode} - Users表是否存在: {userTableExists}");

            // 创建表示例
            string createUsersTableSql = @"
                CREATE TABLE IF NOT EXISTS users (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    username VARCHAR(50) UNIQUE NOT NULL,
                    email VARCHAR(100) UNIQUE NOT NULL,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await db.CreateTableIfNotExistsAsync("users", createUsersTableSql);

            // 插入数据示例
            var insertParams = new Dictionary<string, object>
            {
                { "@username", $"testuser_{Guid.NewGuid().ToString("N").Substring(0, 8)}" },
                { "@email", $"test_{DateTime.Now.Ticks}@example.com" }
            };

            int rowsAffected = await db.ExecuteNonQueryAsync(
                "INSERT INTO users (username, email) VALUES (@username, @email)",
                insertParams
            );
            LogUtils.Info($"{mode} - 插入了 {rowsAffected} 行数据");

            // 查询数据示例
            var users = await db.ExecuteQueryAsync("SELECT * FROM users WHERE username = @username", insertParams);
            LogUtils.Info($"{mode} - 查询到 {users.Rows.Count} 条用户记录");

            // 并发测试（展示连接池优势）
            if (mode.Contains("连接池"))
            {
                await ConcurrentTest(db);
            }

            // 关闭连接
            await db.CloseAsync();
            
            LogUtils.Info($"{mode} 测试完成");
        }
        catch (Exception ex)
        {
            LogUtils.Error($"{mode} 操作失败", ex);
            throw;
        }
    }
    
    /// <summary>
    /// 并发测试，展示连接池的优势
    /// </summary>
    private static async Task ConcurrentTest(Database db)
    {
        LogUtils.Info("开始并发测试（展示连接池效果）");
        
        var tasks = new List<Task>();
        var startTime = DateTime.Now;
        
        // 创建10个并发任务
        for (int i = 0; i < 10; i++)
        {
            int taskId = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    // 每个任务执行几次数据库操作
                    for (int j = 0; j < 3; j++)
                    {
                        await db.OpenAsync();
                        
                        // 简单查询
                        var result = await db.ExecuteScalarAsync("SELECT COUNT(*) FROM users");
                        LogUtils.Debug($"任务 {taskId}, 查询 {j+1}: 用户总数 {result}");
                        
                        await Task.Delay(50); // 模拟业务处理时间
                        await db.CloseAsync();
                    }
                }
                catch (Exception ex)
                {
                    LogUtils.Error($"并发任务 {taskId} 失败", ex);
                }
            }));
        }
        
        // 等待所有任务完成
        await Task.WhenAll(tasks);
        
        var duration = DateTime.Now - startTime;
        LogUtils.Info($"并发测试完成，耗时: {duration.TotalMilliseconds:F2}ms");
    }

    /// <summary>
    /// 综合使用示例
    /// </summary>
    public static async Task ComprehensiveExample()
    {
        // 初始化日志
        LogUtils.Initialize(LogUtils.LogLevel.Info, @"logs\comprehensive.log");
        
        // 加载配置
        SimpleConfig.LoadEnv();
        SimpleConfig.Initialize("Development");

        LogUtils.Info("=== 开始综合示例 ===");

        try
        {
            // 配置管理示例
            ConfigExample.RunExample();
            
            // 数据库操作
            await DatabaseExample();
            
            // 模拟业务逻辑
            await SimulateBusinessLogic();
        }
        catch (Exception ex)
        {
            LogUtils.Error("综合示例执行失败", ex);
        }

        LogUtils.Info("=== 综合示例结束 ===");
    }

    private static async Task SimulateBusinessLogic()
    {
        LogUtils.Info("开始模拟业务逻辑...");

        // 模拟一些业务操作
        for (int i = 0; i < 5; i++)
        {
            LogUtils.Debug("处理业务项 {0}", i + 1);
            await Task.Delay(100); // 模拟处理时间
        }

        LogUtils.Info("业务逻辑处理完成");
    }
}