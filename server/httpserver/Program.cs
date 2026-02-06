using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Cube.Shared.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Cube.HttpServer;

/// <summary>
/// HTTP 服务器
/// 负责处理游戏内的 HTTP 请求，包括登录、交易、数据查询等
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        // 初始化配置系统
        InitializeConfiguration();
        
        // 初始化JWT工具
        JwtUtils.Initialize();
        
        var builder = WebApplication.CreateBuilder(args);

        // 添加服务
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        // 注册Database服务
        builder.Services.AddSingleton(sp =>
        {
            // 优先使用环境变量配置
            var host = SimpleConfig.GetString("DATABASE_HOST") ?? "localhost";
            var port = SimpleConfig.GetInt("DATABASE_PORT", 3306);
            var database = SimpleConfig.GetRequiredString("DATABASE_NAME");
            var username = SimpleConfig.GetRequiredString("DATABASE_USER");
            var password = SimpleConfig.GetRequiredString("DATABASE_PASSWORD");
            
            return Database.Create(host, database, username, password, port);
        });

        // 添加JWT认证
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SimpleConfig.GetRequiredString("Jwt:SecretKey"))),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };
            });

        // 添加 CORS 支持
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        var app = builder.Build();

        // 配置中间件
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors();
        // 在开发环境中暂时禁用HTTPS重定向以便测试
        if (app.Environment.IsProduction())
        {
            app.UseHttpsRedirection();
        }
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
    
    /// <summary>
    /// 初始化配置系统
    /// </summary>
    private static void InitializeConfiguration()
    {
        try
        {
            // 加载.env文件
            var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
            if (File.Exists(envPath))
            {
                SimpleConfig.LoadEnv(envPath);
            }
            else
            {
                // 如果找不到.env文件，尝试在当前目录查找
                SimpleConfig.LoadEnv();
            }
            
            // 初始化配置（根据环境变量确定环境）
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            SimpleConfig.Initialize(environment);
            
            Logger.LogInfo($"Configuration initialized for environment: {environment}");
            
            // 调试：打印关键环境变量
            Logger.LogInfo($"DATABASE_NAME: {Environment.GetEnvironmentVariable("DATABASE_NAME")}");
            Logger.LogInfo($"DATABASE_USER: {Environment.GetEnvironmentVariable("DATABASE_USER")}");
            Logger.LogInfo($"DATABASE_PASSWORD: {Environment.GetEnvironmentVariable("DATABASE_PASSWORD")}");
            
            // 验证必要配置
            ValidateRequiredConfiguration();
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to initialize configuration", ex);
            throw;
        }
    }
    
    /// <summary>
    /// 验证必需的配置项
    /// </summary>
    private static void ValidateRequiredConfiguration()
    {
        var requiredConfigs = new[]
        {
            "Jwt:SecretKey",
            "DATABASE_NAME",
            "DATABASE_USER", 
            "DATABASE_PASSWORD"
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
            Logger.LogError($"Missing required configurations: {string.Join(", ", missingConfigs)}");
            throw new InvalidOperationException($"Missing required configurations: {string.Join(", ", missingConfigs)}");
        }
        
        Logger.LogInfo("Configuration validation passed");
    }
}
