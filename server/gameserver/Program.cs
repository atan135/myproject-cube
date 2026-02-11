using Cube.GameServer.KcpTransport;
using Cube.Shared.Utils;

namespace Cube.GameServer;

/// <summary>
/// 游戏服务器 — 双协议架构
///
/// 1. MagicOnion (gRPC/TCP) — 技能、战斗等敏感操作，可靠传输
/// 2. KCP (UDP) — 位移同步，使用快照插值，低延迟传输
///
/// 所有游戏逻辑计算信任服务端（Server-Authoritative）
/// </summary>
public class Program
{
    private static KcpMovementServer? _kcpMovementServer;
    private static WebApplication? _magicOnionApp;
    private static bool _isRunning = false;

    public static void Main(string[] args)
    {
        // 初始化配置系统
        InitializeConfiguration();

        Logger.LogInfo("=== Game Server Starting (Dual Protocol) ===");

        // 读取配置
        var magicOnionPort = SimpleConfig.GetInt("GameServer:MagicOnionPort", 5001);
        var kcpPort = SimpleConfig.GetInt("GameServer:KcpPort", 7777);
        var snapshotRate = SimpleConfig.GetInt("GameServer:SnapshotRate", 20);

        // 显示服务器配置
        DisplayServerInfo();

        // 1. 启动 MagicOnion (gRPC/TCP) 服务
        StartMagicOnionServer(magicOnionPort);

        // 2. 启动 KCP (UDP) 位移同步服务
        StartKcpMovementServer((ushort)kcpPort, snapshotRate);

        _isRunning = true;

        Logger.LogInfo("=== All services started ===");
        Logger.LogInfo($"  MagicOnion (TCP): port {magicOnionPort}");
        Logger.LogInfo($"  KCP Movement (UDP): port {kcpPort}");
        Logger.LogInfo($"  Snapshot Rate: {snapshotRate} Hz");
        Logger.LogInfo("Press Ctrl+C to stop the server...");

        // 优雅关闭
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            // 阻塞直到收到退出信号
            Task.Delay(Timeout.Infinite, cts.Token).Wait();
        }
        catch (AggregateException) { /* Ctrl+C triggered */ }

        StopServer();
    }

    /// <summary>
    /// 启动 MagicOnion gRPC 服务
    /// 用于技能释放、战斗计算等需要可靠传输的操作
    /// </summary>
    private static void StartMagicOnionServer(int port)
    {
        try
        {
            var builder = WebApplication.CreateBuilder();

            // 配置 Kestrel 监听指定端口
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(port, listenOptions =>
                {
                    listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
                });
            });

            // 添加 MagicOnion 服务
            builder.Services.AddGrpc();
            builder.Services.AddMagicOnion();

            _magicOnionApp = builder.Build();

            // 映射 MagicOnion 服务端点
            _magicOnionApp.MapMagicOnionService();

            // 异步启动
            _ = Task.Run(async () =>
            {
                try
                {
                    await _magicOnionApp.RunAsync();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"MagicOnion server error: {ex.Message}");
                }
            });

            Logger.LogInfo($"[MagicOnion] gRPC server starting on port {port} (HTTP/2)");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to start MagicOnion server on port {port}", ex);
        }
    }

    /// <summary>
    /// 启动 KCP 位移同步服务
    /// 使用 UDP + KCP 可靠层进行位移数据的低延迟传输
    /// </summary>
    private static void StartKcpMovementServer(ushort port, int snapshotRate)
    {
        try
        {
            _kcpMovementServer = new KcpMovementServer(port, snapshotRate);
            _kcpMovementServer.Start();
            Logger.LogInfo($"[KCP] Movement sync server started on UDP port {port}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to start KCP server on port {port}", ex);
        }
    }

    /// <summary>
    /// 停止所有服务
    /// </summary>
    private static void StopServer()
    {
        _isRunning = false;

        Logger.LogInfo("Stopping all services...");

        // 停止 KCP 服务
        _kcpMovementServer?.Stop();
        Logger.LogInfo("[KCP] Movement server stopped");

        // 停止 MagicOnion
        _magicOnionApp?.StopAsync().Wait(TimeSpan.FromSeconds(5));
        Logger.LogInfo("[MagicOnion] gRPC server stopped");

        Logger.LogInfo("=== Game Server stopped ===");
    }

    /// <summary>
    /// 初始化配置系统
    /// </summary>
    private static void InitializeConfiguration()
    {
        try
        {
            SimpleConfig.LoadEnv();
            SimpleConfig.Initialize("Development");

            Logger.LogInfo("Game server configuration initialized");

            // 验证必要配置
            ValidateRequiredConfiguration();
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to initialize game server configuration", ex);
            throw;
        }
    }

    /// <summary>
    /// 验证必需的配置项
    /// </summary>
    private static void ValidateRequiredConfiguration()
    {
        var kcpPort = SimpleConfig.GetInt("GameServer:KcpPort", 7777);
        if (kcpPort <= 0 || kcpPort > 65535)
        {
            Logger.LogError("GameServer:KcpPort configuration is invalid");
            throw new InvalidOperationException("GameServer:KcpPort configuration is required");
        }

        Logger.LogInfo("Game server configuration validation passed");
    }

    /// <summary>
    /// 显示服务器配置信息
    /// </summary>
    private static void DisplayServerInfo()
    {
        var magicOnionPort = SimpleConfig.GetInt("GameServer:MagicOnionPort", 5001);
        var kcpPort = SimpleConfig.GetInt("GameServer:KcpPort", 7777);
        var snapshotRate = SimpleConfig.GetInt("GameServer:SnapshotRate", 20);
        var maxClients = SimpleConfig.GetInt("GameServer:MaxClientsPerRoom", 8);

        Logger.LogInfo("=== Game Server Configuration ===");
        Logger.LogInfo($"  MagicOnion Port (TCP/gRPC): {magicOnionPort}");
        Logger.LogInfo($"  KCP Port (UDP): {kcpPort}");
        Logger.LogInfo($"  Snapshot Rate: {snapshotRate} Hz");
        Logger.LogInfo($"  Max Clients Per Room: {maxClients}");
        Logger.LogInfo("=================================");
    }
}
