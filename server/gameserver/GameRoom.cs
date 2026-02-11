using Cube.GameServer.KcpTransport;
using Cube.Shared.Utils;

namespace Cube.GameServer;

/// <summary>
/// 游戏房间
/// 管理单个游戏房间的状态和逻辑
/// 
/// 双协议集成：
/// - MagicOnion (TCP) 处理技能/战斗等可靠消息
/// - KcpMovementServer (UDP) 处理位移同步
/// </summary>
public class GameRoom
{
    public string RoomId { get; private set; }
    public List<GameClient> Clients { get; private set; }
    public bool IsRunning { get; private set; }
    private int _tickRate = 20; // 20 ticks per second
    private Timer? _gameLoopTimer;

    /// <summary>
    /// KCP 位移同步服务器引用
    /// 由 Program.cs 创建并注入
    /// </summary>
    public KcpMovementServer? MovementServer { get; set; }

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
    /// 更新游戏状态
    /// 位移同步由 KcpMovementServer 独立处理
    /// 这里只需处理技能/战斗等逻辑
    /// </summary>
    private void UpdateGameState(object? state)
    {
        if (!IsRunning) return;

        // TODO: 处理游戏逻辑
        // 1. 处理技能/战斗计算（通过 MagicOnion 接收指令）
        // 2. 检查碰撞/伤害等
        // 3. 通过 MagicOnion 广播战斗结果
        //
        // 注意：位移相关逻辑已移至 KcpMovementServer
        // 如需获取玩家位置进行战斗判定，可通过 MovementServer.GetPlayerState() 获取
    }

    /// <summary>
    /// 获取指定玩家的当前位置（从KCP服务获取）
    /// 用于技能/战斗计算时需要位置信息的场景
    /// </summary>
    public ServerPlayerState? GetPlayerPosition(int playerId)
    {
        return MovementServer?.GetPlayerState(playerId);
    }
}

/// <summary>
/// 游戏客户端
/// </summary>
public class GameClient
{
    public string ClientId { get; set; } = string.Empty;
    public long UserId { get; set; }
    public int PlayerId { get; set; }
    // TODO: 添加更多客户端状态信息
}
