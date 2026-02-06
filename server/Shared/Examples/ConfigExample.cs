using Cube.Shared.Utils;

namespace Cube.Shared.Examples;

/// <summary>
/// 简化配置管理使用示例
/// </summary>
public static class ConfigExample
{
    /// <summary>
    /// 配置管理器使用示例
    /// </summary>
    public static void RunExample()
    {
        // 1. 加载.env文件（类似Node.js dotenv）
        SimpleConfig.LoadEnv();
        
        // 2. 初始化配置系统
        SimpleConfig.Initialize("Development");
        
        LogUtils.Info("=== 简化配置管理示例 ===");
        
        // 3. 获取不同类型的配置值
        var dbHost = SimpleConfig.GetString("Database:Host", "localhost");
        var dbPort = SimpleConfig.GetInt("Database:Port", 3306);
        var enableDebug = SimpleConfig.GetBool("Logging:EnableDebug", false);
        
        LogUtils.Info($"数据库配置 - Host: {dbHost}, Port: {dbPort}, Debug: {enableDebug}");
        
        // 4. 获取必需的配置值
        try
        {
            var jwtSecret = SimpleConfig.GetRequiredString("Jwt:SecretKey");
            LogUtils.Info($"JWT密钥长度: {jwtSecret.Length} 字符");
        }
        catch (InvalidOperationException ex)
        {
            LogUtils.Warning($"配置警告: {ex.Message}");
        }
        
        // 5. 获取连接字符串
        var defaultConn = SimpleConfig.GetConnectionString("DefaultConnection");
        LogUtils.Info($"默认连接字符串: {defaultConn ?? "未配置"}");
        
        // 6. 显示部分配置（调试用）
        LogUtils.Debug("=== 部分配置项 ===");
        var allConfigs = SimpleConfig.GetAll();
        var displayKeys = new[] { "Database:Host", "Database:Port", "Jwt:Issuer", "GameServer:Port" };
        
        foreach (var key in displayKeys)
        {
            if (allConfigs.TryGetValue(key, out var value))
            {
                LogUtils.Debug($"{key}: {value}");
            }
        }
        
        // 7. 环境变量访问
        var envName = Environment.GetEnvironmentVariable("APP_ENVIRONMENT");
        LogUtils.Info($"环境变量 APP_ENVIRONMENT: {envName ?? "未设置"}");
        
        LogUtils.Info("=== 配置示例完成 ===");
    }
    
    /// <summary>
    /// 在实际应用中的使用方式
    /// </summary>
    public static Database CreateDatabaseFromConfig()
    {
        // 从配置创建数据库连接
        return Database.Create(
            host: SimpleConfig.GetString("Database:Host") ?? "localhost",
            database: SimpleConfig.GetString("Database:Name") ?? "cube_game", 
            username: SimpleConfig.GetString("Database:User") ?? "root",
            password: SimpleConfig.GetString("Database:Password") ?? "password",
            port: SimpleConfig.GetInt("Database:Port", 3306)
        );
    }
    
    /// <summary>
    /// 配置验证示例
    /// </summary>
    public static void ValidateConfiguration()
    {
        var requiredConfigs = new[]
        {
            "Database:Host",
            "Database:Name", 
            "Jwt:SecretKey"
        };
        
        var missingConfigs = new List<string>();
        
        foreach (var configKey in requiredConfigs)
        {
            var value = SimpleConfig.GetString(configKey);
            if (string.IsNullOrEmpty(value))
            {
                missingConfigs.Add(configKey);
            }
        }
        
        if (missingConfigs.Any())
        {
            LogUtils.Error($"缺少必需配置: {string.Join(", ", missingConfigs)}");
            throw new InvalidOperationException($"缺少必需配置项: {string.Join(", ", missingConfigs)}");
        }
        
        LogUtils.Info("配置验证通过");
    }
}