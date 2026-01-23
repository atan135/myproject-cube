using Cube.Shared.Utils;

namespace Cube.GameServer;

/// <summary>
/// 游戏房间
/// 管理单个游戏房间的状态和逻辑
/// </summary>
public class GameRoom
{
    public string RoomId { get; private set; }
    public List<GameClient> Clients { get; private set; }
    public bool IsRunning { get; private set; }
    private int _tickRate = 20; // 20 ticks per second
    private Timer? _gameLoopTimer;

    public GameRoom(string roomId)
    {
        RoomId = roomId;
        Clients = new List<GameClient>();
        IsRunning = false;
    }

    /// <summary>
    /// 添加客户端到房间
    /// </summary>
    public void AddClient(GameClient client)
    {
        Clients.Add(client);
        Logger.LogInfo($"Client {client.ClientId} joined room {RoomId}");
    }

    /// <summary>
    /// 从房间移除客户端
    /// </summary>
    public void RemoveClient(GameClient client)
    {
        Clients.Remove(client);
        Logger.LogInfo($"Client {client.ClientId} left room {RoomId}");
    }

    /// <summary>
    /// 开始游戏
    /// </summary>
    public void StartGame()
    {
        if (IsRunning) return;

        IsRunning = true;
        int tickInterval = 1000 / _tickRate;

        _gameLoopTimer = new Timer(UpdateGameState, null, 0, tickInterval);
        Logger.LogInfo($"Game started in room {RoomId}");
    }

    /// <summary>
    /// 停止游戏
    /// </summary>
    public void StopGame()
    {
        IsRunning = false;
        _gameLoopTimer?.Dispose();
        Logger.LogInfo($"Game stopped in room {RoomId}");
    }

    /// <summary>
    /// 更新游戏状态（帧同步）
    /// </summary>
    private void UpdateGameState(object? state)
    {
        if (!IsRunning) return;

        // TODO: 处理游戏逻辑
        // 1. 收集所有客户端输入
        // 2. 执行游戏逻辑
        // 3. 广播状态更新给所有客户端
    }
}

/// <summary>
/// 游戏客户端
/// </summary>
public class GameClient
{
    public string ClientId { get; set; } = string.Empty;
    public long UserId { get; set; }
    // TODO: 添加更多客户端状态信息
}
