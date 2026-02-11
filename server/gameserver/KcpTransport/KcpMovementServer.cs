using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using kcp2k;
using Cube.Shared.Utils;

namespace Cube.GameServer.KcpTransport;

/// <summary>
/// 服务端玩家状态（权威状态）
/// 所有位移计算以服务端为准
/// </summary>
public class ServerPlayerState
{
    public int PlayerId { get; set; }
    public int KcpConnectionId { get; set; }
    public Vector3Data Position { get; set; }
    public QuaternionData Rotation { get; set; }
    public Vector3Data Velocity { get; set; }
    public bool IsGrounded { get; set; } = true;
    public uint LastInputSequence { get; set; }
    public long LastUpdateTime { get; set; }

    /// <summary>最后收到的移动输入</summary>
    public MovementInputMessage? LastInput { get; set; }
}

/// <summary>
/// KCP 位移同步服务器
/// 基于 KcpServer 实现的高层位移同步服务
///
/// 架构说明：
/// - 使用独立线程运行 KCP tick 循环（高频率 10ms）
/// - 客户端通过 KCP unreliable 通道发送位移输入
/// - 服务端以固定频率（默认 20Hz）广播世界快照
/// - 快照包含所有玩家的权威位置、旋转、速度
/// - 客户端收到快照后使用快照插值进行平滑展示
/// </summary>
public class KcpMovementServer : IDisposable
{
    private readonly KcpServer _kcpServer;
    private readonly KcpConfig _kcpConfig;
    private Thread? _tickThread;
    private volatile bool _isRunning;

    // 玩家状态管理
    // connectionId -> ServerPlayerState
    private readonly ConcurrentDictionary<int, ServerPlayerState> _playersByConnection = new();
    // playerId -> ServerPlayerState
    private readonly ConcurrentDictionary<int, ServerPlayerState> _playersByPlayerId = new();

    // 快照广播
    private readonly int _snapshotRate; // 快照发送频率 (Hz)
    private uint _serverTick;
    private readonly Stopwatch _serverClock = new();
    private long _lastSnapshotTime;

    // 发送缓冲区（复用，避免GC）
    private readonly byte[] _sendBuffer = new byte[1200]; // MTU大小

    // 简单的物理常数（可配置）
    private const float GRAVITY = -9.81f;
    private const float GROUND_Y = 0f; // 地面高度
    private const float MAX_MOVE_SPEED = 10f; // 最大移动速度（防作弊）
    private const float DELTA_TIME_MS = 10f; // tick间隔 (ms)

    /// <summary>当前连接数</summary>
    public int ConnectionCount => _playersByConnection.Count;

    /// <summary>
    /// 创建 KCP 位移同步服务器
    /// </summary>
    /// <param name="port">KCP 监听端口（UDP）</param>
    /// <param name="snapshotRate">快照广播频率（Hz），默认20</param>
    public KcpMovementServer(ushort port, int snapshotRate = 20)
    {
        _snapshotRate = snapshotRate;

        // 配置 KCP：低延迟模式
        _kcpConfig = new KcpConfig(
            DualMode: false,        // 只用 IPv4 简化测试
            NoDelay: true,          // 启用 NoDelay 降低延迟
            Interval: 10,           // 10ms tick 间隔
            FastResend: 2,          // 快速重传
            CongestionWindow: false,// 关闭拥塞窗口
            SendWindowSize: 128,
            ReceiveWindowSize: 128,
            Timeout: 10000,         // 10秒超时
            MaxRetransmits: Kcp.DEADLINK
        );

        _kcpServer = new KcpServer(
            OnConnected,
            OnData,
            OnDisconnected,
            OnError,
            _kcpConfig
        );

        // 启动 KCP 服务
        _kcpServer.Start(port);
        Logger.LogInfo($"[KcpMovement] Server started on UDP port {port}, snapshot rate: {snapshotRate}Hz");
    }

    /// <summary>
    /// 启动 tick 循环线程
    /// </summary>
    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        _serverClock.Start();
        _lastSnapshotTime = 0;
        _serverTick = 0;

        _tickThread = new Thread(TickLoop)
        {
            Name = "KcpMovement-Tick",
            IsBackground = true
        };
        _tickThread.Start();

        Logger.LogInfo("[KcpMovement] Tick loop started");
    }

    /// <summary>
    /// 主 tick 循环
    /// 以约 10ms 间隔运行，处理网络IO和游戏逻辑
    /// </summary>
    private void TickLoop()
    {
        while (_isRunning)
        {
            try
            {
                // 1. 处理网络输入
                _kcpServer.TickIncoming();

                // 2. 更新所有玩家物理状态
                UpdatePhysics();

                // 3. 按频率发送快照
                long now = _serverClock.ElapsedMilliseconds;
                long snapshotIntervalMs = 1000 / _snapshotRate;
                if (now - _lastSnapshotTime >= snapshotIntervalMs)
                {
                    _serverTick++;
                    BroadcastWorldSnapshot();
                    _lastSnapshotTime = now;
                }

                // 4. 处理网络输出
                _kcpServer.TickOutgoing();
            }
            catch (Exception ex)
            {
                Logger.LogError($"[KcpMovement] Tick error: {ex.Message}");
            }

            // 等待约 10ms（与 KCP interval 对齐）
            Thread.Sleep(10);
        }
    }

    /// <summary>
    /// 更新所有玩家的物理状态
    /// 服务端权威：根据最后收到的输入计算位置
    /// </summary>
    private void UpdatePhysics()
    {
        float dt = DELTA_TIME_MS / 1000f;

        foreach (var kvp in _playersByConnection)
        {
            var player = kvp.Value;
            if (player.LastInput == null) continue;

            var input = player.LastInput.Value;

            // 验证并限制移动速度（防作弊）
            float speed = Math.Min(Math.Abs(input.MoveSpeed), MAX_MOVE_SPEED);

            // 计算水平移动
            var pos = player.Position;
            var vel = player.Velocity;

            float moveX = input.Direction.X * speed * dt;
            float moveZ = input.Direction.Z * speed * dt;

            pos = new Vector3Data(
                pos.X + moveX,
                pos.Y + vel.Y * dt,
                pos.Z + moveZ
            );

            // 简单重力
            if (!player.IsGrounded)
            {
                vel = new Vector3Data(vel.X, vel.Y + GRAVITY * dt, vel.Z);
            }

            // 跳跃
            if (input.IsJumping && player.IsGrounded)
            {
                vel = new Vector3Data(vel.X, 5f, vel.Z); // 跳跃初速度
                player.IsGrounded = false;
            }

            // 地面检测
            if (pos.Y <= GROUND_Y)
            {
                pos = new Vector3Data(pos.X, GROUND_Y, pos.Z);
                vel = new Vector3Data(vel.X, 0, vel.Z);
                player.IsGrounded = true;
            }

            // 计算旋转（简单的 Y 轴旋转）
            float yaw = input.YawAngle;
            var rotation = new QuaternionData(0, MathF.Sin(yaw / 2f), 0, MathF.Cos(yaw / 2f));

            // 更新权威状态
            player.Position = pos;
            player.Velocity = vel;
            player.Rotation = rotation;
            player.LastUpdateTime = _serverClock.ElapsedMilliseconds;
        }
    }

    /// <summary>
    /// 广播世界快照给所有已连接的客户端
    /// </summary>
    private void BroadcastWorldSnapshot()
    {
        if (_playersByConnection.IsEmpty) return;

        // 构建快照
        var players = new List<PlayerSnapshotData>();
        foreach (var kvp in _playersByConnection)
        {
            var player = kvp.Value;
            players.Add(new PlayerSnapshotData
            {
                PlayerId = player.PlayerId,
                Position = player.Position,
                Rotation = player.Rotation,
                Velocity = player.Velocity,
                IsGrounded = player.IsGrounded
            });
        }

        var snapshot = new WorldSnapshotMessage
        {
            ServerTick = _serverTick,
            Timestamp = _serverClock.ElapsedMilliseconds,
            Players = players.ToArray()
        };

        // 检查大小是否超过 MTU
        int size = snapshot.CalculateSize();
        if (size > _sendBuffer.Length)
        {
            Logger.LogError($"[KcpMovement] Snapshot too large: {size} bytes > {_sendBuffer.Length} MTU. Players: {players.Count}");
            return;
        }

        int written = snapshot.WriteTo(_sendBuffer, 0);

        // 广播给所有客户端（unreliable 通道，丢包没关系）
        var segment = new ArraySegment<byte>(_sendBuffer, 0, written);
        foreach (var kvp in _playersByConnection)
        {
            try
            {
                _kcpServer.Send(kvp.Key, segment, KcpChannel.Unreliable);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[KcpMovement] Failed to send snapshot to connection {kvp.Key}: {ex.Message}");
            }
        }
    }

    #region KCP 回调

    private void OnConnected(int connectionId)
    {
        Logger.LogInfo($"[KcpMovement] KCP connection established: {connectionId}");
        // 注意：此时只是KCP握手完成，还需要等待 PlayerJoin 消息来绑定 PlayerId
    }

    private void OnData(int connectionId, ArraySegment<byte> data, KcpChannel channel)
    {
        if (data.Count < 1) return;

        var msgType = MessageParser.ParseType(data);

        switch (msgType)
        {
            case KcpMessageType.PlayerJoin:
                HandlePlayerJoin(connectionId, data);
                break;
            case KcpMessageType.MovementInput:
                HandleMovementInput(connectionId, data);
                break;
            default:
                Logger.LogInfo($"[KcpMovement] Unknown message type: {msgType} from connection {connectionId}");
                break;
        }
    }

    private void OnDisconnected(int connectionId)
    {
        Logger.LogInfo($"[KcpMovement] KCP disconnected: {connectionId}");

        if (_playersByConnection.TryRemove(connectionId, out var player))
        {
            _playersByPlayerId.TryRemove(player.PlayerId, out _);

            // 通知其他客户端该玩家离开
            var leaveMsg = new PlayerLeaveMessage { PlayerId = player.PlayerId };
            leaveMsg.WriteTo(_sendBuffer, 0);
            var segment = new ArraySegment<byte>(_sendBuffer, 0, PlayerLeaveMessage.SIZE);

            foreach (var kvp in _playersByConnection)
            {
                try
                {
                    _kcpServer.Send(kvp.Key, segment, KcpChannel.Reliable);
                }
                catch { /* ignore */ }
            }

            Logger.LogInfo($"[KcpMovement] Player {player.PlayerId} removed (was connection {connectionId})");
        }
    }

    private void OnError(int connectionId, ErrorCode error, string message)
    {
        Logger.LogError($"[KcpMovement] Error on connection {connectionId}: [{error}] {message}");
    }

    #endregion

    #region 消息处理

    private void HandlePlayerJoin(int connectionId, ArraySegment<byte> data)
    {
        if (data.Count < PlayerJoinMessage.SIZE) return;

        var msg = PlayerJoinMessage.ReadFrom(data.Array!, data.Offset);
        Logger.LogInfo($"[KcpMovement] Player {msg.PlayerId} joining via connection {connectionId}");

        var playerState = new ServerPlayerState
        {
            PlayerId = msg.PlayerId,
            KcpConnectionId = connectionId,
            Position = new Vector3Data(0, 0, 0),
            Rotation = QuaternionData.Identity,
            Velocity = new Vector3Data(0, 0, 0),
            IsGrounded = true,
            LastUpdateTime = _serverClock.ElapsedMilliseconds
        };

        _playersByConnection[connectionId] = playerState;
        _playersByPlayerId[msg.PlayerId] = playerState;

        // 发送确认
        var ack = new PlayerJoinAckMessage
        {
            PlayerId = msg.PlayerId,
            ServerTick = _serverTick
        };
        ack.WriteTo(_sendBuffer, 0);
        _kcpServer.Send(connectionId, new ArraySegment<byte>(_sendBuffer, 0, PlayerJoinAckMessage.SIZE), KcpChannel.Reliable);

        Logger.LogInfo($"[KcpMovement] Player {msg.PlayerId} joined successfully. Total players: {_playersByConnection.Count}");
    }

    private void HandleMovementInput(int connectionId, ArraySegment<byte> data)
    {
        if (data.Count < MovementInputMessage.SIZE) return;

        var msg = MovementInputMessage.ReadFrom(data.Array!, data.Offset);

        if (_playersByConnection.TryGetValue(connectionId, out var player))
        {
            // 验证 PlayerId 匹配
            if (player.PlayerId != msg.PlayerId)
            {
                Logger.LogError($"[KcpMovement] PlayerId mismatch! Connection {connectionId} claims to be {msg.PlayerId} but registered as {player.PlayerId}");
                return;
            }

            // 防止旧输入覆盖新输入（网络乱序）
            if (msg.InputSequence <= player.LastInputSequence)
                return;

            player.LastInputSequence = msg.InputSequence;
            player.LastInput = msg;
        }
    }

    #endregion

    /// <summary>
    /// 获取指定玩家的当前状态
    /// </summary>
    public ServerPlayerState? GetPlayerState(int playerId)
    {
        _playersByPlayerId.TryGetValue(playerId, out var state);
        return state;
    }

    /// <summary>
    /// 获取所有在线玩家状态
    /// </summary>
    public IEnumerable<ServerPlayerState> GetAllPlayerStates()
    {
        return _playersByConnection.Values;
    }

    /// <summary>
    /// 停止服务器
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
        _tickThread?.Join(3000);
        _kcpServer.Stop();
        _serverClock.Stop();
        _playersByConnection.Clear();
        _playersByPlayerId.Clear();
        Logger.LogInfo("[KcpMovement] Server stopped");
    }

    public void Dispose()
    {
        Stop();
    }
}
