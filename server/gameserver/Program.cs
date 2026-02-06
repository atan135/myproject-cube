using System.Net;
using System.Net.Sockets;
using Cube.Shared.Protocols;
using Cube.Shared.Utils;

namespace Cube.GameServer;

/// <summary>
/// 游戏服务器
/// 负责处理游戏内的帧同步请求、游戏逻辑处理、状态同步
/// </summary>
public class Program
{
    private static TcpListener? _listener;
    private static bool _isRunning = false;

    public static void Main(string[] args)
    {
        // 初始化配置系统
        InitializeConfiguration();
        
        Logger.LogInfo("Game Server starting...");

        // 从配置获取端口
        var port = SimpleConfig.GetInt("GameServer:Port", 8888);
        
        // 启动 TCP 服务器
        StartTcpServer(port);

        // 保持服务器运行
        Console.WriteLine("Press any key to stop the server...");
        Console.ReadKey();

        StopServer();
    }

    /// <summary>
    /// 启动 TCP 服务器
    /// </summary>
    private static void StartTcpServer(int port)
    {
        try
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _isRunning = true;

            Logger.LogInfo($"Game Server listening on port {port}");
            
            // 显示服务器配置信息
            DisplayServerInfo();
            
            // 显示服务器配置信息
            DisplayServerInfo();

            // 异步接受客户端连接
            _ = Task.Run(async () =>
            {
                while (_isRunning)
                {
                    try
                    {
                        var client = await _listener.AcceptTcpClientAsync();
                        _ = Task.Run(() => HandleClient(client));
                    }
                    catch (Exception ex)
                    {
                        if (_isRunning)
                        {
                            Logger.LogError("Error accepting client", ex);
                        }
                    }
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to start server on port {port}", ex);
        }
    }

    /// <summary>
    /// 处理客户端连接
    /// </summary>
    private static async Task HandleClient(TcpClient client)
    {
        var clientEndPoint = client.Client.RemoteEndPoint;
        Logger.LogInfo($"Client connected: {clientEndPoint}");

        try
        {
            var stream = client.GetStream();
            var buffer = new byte[4096];

            while (client.Connected && _isRunning)
            {
                // 读取消息
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    break; // 客户端断开连接
                }

                // TODO: 解析和处理消息
                Logger.LogInfo($"Received {bytesRead} bytes from {clientEndPoint}");

                // TODO: 处理游戏逻辑和状态同步
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error handling client {clientEndPoint}", ex);
        }
        finally
        {
            client.Close();
            Logger.LogInfo($"Client disconnected: {clientEndPoint}");
        }
    }

    /// <summary>
    /// 停止服务器
    /// </summary>
    private static void StopServer()
    {
        _isRunning = false;
        _listener?.Stop();
        Logger.LogInfo("Game Server stopped");
    }
    
    /// <summary>
    /// 初始化配置系统
    /// </summary>
    private static void InitializeConfiguration()
    {
        try
        {
            // 加载.env文件
            SimpleConfig.LoadEnv();
            
            // 初始化配置
            SimpleConfig.Initialize("Development");
            
            Logger.LogInfo("Game server configuration initialized");
            
            // 调试：显示所有配置
            Logger.LogInfo("=== All Configurations ===");
            var allConfigs = SimpleConfig.GetAll();
            foreach (var config in allConfigs.Take(10))
            {
                Logger.LogInfo($"{config.Key}: {config.Value}");
            }
            Logger.LogInfo("=========================");
            
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
        // 检查游戏服务器配置
        var gameServerPort = SimpleConfig.GetInt("GameServer:Port");
        if (gameServerPort <= 0)
        {
            Logger.LogError("GameServer:Port configuration is missing or invalid");
            throw new InvalidOperationException("GameServer:Port configuration is required");
        }
        
        Logger.LogInfo("Game server configuration validation passed");
    }
    
    /// <summary>
    /// 显示服务器配置信息
    /// </summary>
    private static void DisplayServerInfo()
    {
        var port = SimpleConfig.GetInt("GameServer:Port", 8888);
        var tickRate = SimpleConfig.GetInt("GameServer:TickRate", 20);
        var maxClients = SimpleConfig.GetInt("GameServer:MaxClientsPerRoom", 8);
        
        Logger.LogInfo("=== Game Server Configuration ===");
        Logger.LogInfo($"Listening Port: {port}");
        Logger.LogInfo($"Tick Rate: {tickRate} FPS");
        Logger.LogInfo($"Max Clients Per Room: {maxClients}");
        Logger.LogInfo("================================");
    }
}
