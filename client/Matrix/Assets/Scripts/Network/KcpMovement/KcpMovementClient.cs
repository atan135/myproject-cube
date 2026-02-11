using System;
using System.Collections.Generic;
using kcp2k;
using UnityEngine;

namespace Cube.Network.KcpMovement
{
    /// <summary>
    /// KCP 位移同步客户端
    ///
    /// 职责：
    /// 1. 通过 KCP 连接服务端 UDP 端口
    /// 2. 发送 PlayerJoin 绑定玩家ID
    /// 3. 高频发送位移输入到服务端 (unreliable)
    /// 4. 接收世界快照，驱动远程玩家的快照插值
    /// 5. 本地玩家使用客户端预测 + 服务端校正
    ///
    /// 使用方式：
    /// 1. 挂载到一个 DontDestroyOnLoad 的 GameObject 上
    /// 2. 在登录成功后调用 Connect(serverIP, port, playerId)
    /// 3. 每帧调用 SendMovementInput() 发送本地玩家输入
    /// 4. 通过 OnWorldSnapshot 事件获取最新世界状态
    /// </summary>
    public class KcpMovementClient : MonoBehaviour
    {
        [Header("连接设置")]
        [SerializeField] private string serverAddress = "127.0.0.1";
        [SerializeField] private ushort serverPort = 7777;

        [Header("玩家设置")]
        [SerializeField] private int playerId = 1;

        [Header("同步设置")]
        [Tooltip("输入发送频率 (Hz)")]
        [SerializeField] private int inputSendRate = 30;

        [Tooltip("快照插值延迟 (秒)")]
        [SerializeField] private float interpolationDelay = 0.1f;

        [Header("调试")]
        [SerializeField] private bool showDebugInfo = true;

        // KCP 客户端
        private KcpClient _kcpClient;
        private KcpConfig _kcpConfig;

        // 状态
        private bool _isConnected;
        private bool _isJoined;
        private uint _inputSequence;
        private float _lastInputSendTime;

        // 发送缓冲区
        private readonly byte[] _sendBuffer = new byte[1200];

        // 远程玩家的快照插值器
        // playerId -> SnapshotInterpolation
        private readonly Dictionary<int, SnapshotInterpolation> _remoteInterpolators = new();

        // 最新收到的快照（用于本地玩家校正）
        private PlayerSnapshotData? _latestLocalSnapshot;

        // 事件
        /// <summary>连接成功时触发</summary>
        public event Action OnConnected;
        /// <summary>断开连接时触发</summary>
        public event Action OnDisconnected;
        /// <summary>加入成功时触发 (playerId, serverTick)</summary>
        public event Action<int, uint> OnJoinConfirmed;
        /// <summary>收到世界快照时触发</summary>
        public event Action<WorldSnapshotMessage> OnWorldSnapshot;
        /// <summary>其他玩家离开时触发</summary>
        public event Action<int> OnPlayerLeft;
        /// <summary>KCP错误时触发</summary>
        public event Action<string> OnKcpError;

        // 公开属性
        public bool IsConnected => _isConnected;
        public bool IsJoined => _isJoined;
        public int PlayerId => playerId;
        public uint LastServerTick { get; private set; }

        private void Awake()
        {
            // KCP配置：低延迟模式，与服务端一致
            _kcpConfig = new KcpConfig(
                DualMode: false,
                NoDelay: true,
                Interval: 10,
                FastResend: 2,
                CongestionWindow: false,
                SendWindowSize: 128,
                ReceiveWindowSize: 128,
                Timeout: 10000,
                MaxRetransmits: Kcp.DEADLINK
            );

            // 重定向 KCP 日志到 Unity 控制台
            kcp2k.Log.Info = Debug.Log;
            kcp2k.Log.Warning = Debug.LogWarning;
            kcp2k.Log.Error = Debug.LogError;
        }

        private void Update()
        {
            // 每帧 tick KCP
            _kcpClient?.Tick();

            // 更新所有远程玩家的插值
            double now = Time.timeAsDouble;
            foreach (var kvp in _remoteInterpolators)
            {
                kvp.Value.Update(now);
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        #region 公开接口

        /// <summary>
        /// 连接到服务端 KCP 位移同步服务
        /// </summary>
        public void Connect(string address, ushort port, int playerIdToUse)
        {
            if (_isConnected)
            {
                Debug.LogWarning("[KcpMovement] Already connected!");
                return;
            }

            serverAddress = address;
            serverPort = port;
            playerId = playerIdToUse;

            _kcpClient = new KcpClient(
                OnKcpConnected,
                OnKcpData,
                OnKcpDisconnected,
                OnKcpErrorCallback,
                _kcpConfig
            );

            Debug.Log($"[KcpMovement] Connecting to {address}:{port} as player {playerId}...");
            _kcpClient.Connect(address, port);
        }

        /// <summary>
        /// 使用 Inspector 中配置的参数连接
        /// </summary>
        public void ConnectWithDefaults()
        {
            Connect(serverAddress, serverPort, playerId);
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            if (_kcpClient != null)
            {
                _kcpClient.Disconnect();
                _kcpClient = null;
            }
            _isConnected = false;
            _isJoined = false;
            _remoteInterpolators.Clear();
        }

        /// <summary>
        /// 发送位移输入到服务端
        /// 应该在 Update 或 FixedUpdate 中每帧调用
        /// </summary>
        /// <param name="direction">移动方向 (归一化，世界坐标)</param>
        /// <param name="moveSpeed">移动速度</param>
        /// <param name="isJumping">是否跳跃</param>
        /// <param name="yawAngle">Y轴旋转角度（弧度）</param>
        public void SendMovementInput(Vector3 direction, float moveSpeed, bool isJumping, float yawAngle)
        {
            if (!_isConnected || !_isJoined) return;

            // 限制发送频率
            float sendInterval = 1f / inputSendRate;
            if (Time.time - _lastInputSendTime < sendInterval) return;
            _lastInputSendTime = Time.time;

            _inputSequence++;

            var msg = new MovementInputMessage
            {
                PlayerId = playerId,
                InputSequence = _inputSequence,
                Direction = Vector3Data.FromUnityVector3(direction),
                MoveSpeed = moveSpeed,
                IsJumping = isJumping,
                YawAngle = yawAngle
            };

            int written = msg.WriteTo(_sendBuffer, 0);
            var segment = new ArraySegment<byte>(_sendBuffer, 0, written);

            // 使用 unreliable 通道发送（位移数据允许丢包）
            _kcpClient.SendData(segment, KcpChannel.Unreliable);
        }

        /// <summary>
        /// 获取远程玩家的插值后位置
        /// </summary>
        public bool TryGetRemotePlayerState(int remotePlayerId, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            if (_remoteInterpolators.TryGetValue(remotePlayerId, out var interpolator))
            {
                position = interpolator.CurrentPosition;
                rotation = interpolator.CurrentRotation;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取所有远程玩家ID
        /// </summary>
        public IEnumerable<int> GetRemotePlayerIds()
        {
            return _remoteInterpolators.Keys;
        }

        /// <summary>
        /// 获取本地玩家最新的服务端校正位置
        /// 用于客户端预测校正
        /// </summary>
        public PlayerSnapshotData? GetLatestLocalSnapshot() => _latestLocalSnapshot;

        #endregion

        #region KCP 回调

        private void OnKcpConnected()
        {
            Debug.Log("[KcpMovement] KCP connected! Sending PlayerJoin...");
            _isConnected = true;
            OnConnected?.Invoke();

            // 自动发送 PlayerJoin
            SendPlayerJoin();
        }

        private void OnKcpData(ArraySegment<byte> data, KcpChannel channel)
        {
            if (data.Count < 1) return;

            var msgType = MessageParser.ParseType(data);

            switch (msgType)
            {
                case KcpMessageType.PlayerJoinAck:
                    HandlePlayerJoinAck(data);
                    break;
                case KcpMessageType.WorldSnapshot:
                    HandleWorldSnapshot(data);
                    break;
                case KcpMessageType.PlayerLeave:
                    HandlePlayerLeave(data);
                    break;
                default:
                    Debug.LogWarning($"[KcpMovement] Unknown message type: {msgType}");
                    break;
            }
        }

        private void OnKcpDisconnected()
        {
            Debug.Log("[KcpMovement] KCP disconnected");
            _isConnected = false;
            _isJoined = false;
            _remoteInterpolators.Clear();
            OnDisconnected?.Invoke();
        }

        private void OnKcpErrorCallback(ErrorCode error, string message)
        {
            Debug.LogError($"[KcpMovement] KCP Error [{error}]: {message}");
            OnKcpError?.Invoke(message);
        }

        #endregion

        #region 消息处理

        private void SendPlayerJoin()
        {
            var msg = new PlayerJoinMessage { PlayerId = playerId };
            int written = msg.WriteTo(_sendBuffer, 0);
            var segment = new ArraySegment<byte>(_sendBuffer, 0, written);

            // PlayerJoin 通过 reliable 通道发送，确保送达
            _kcpClient.SendData(segment, KcpChannel.Reliable);
            Debug.Log($"[KcpMovement] Sent PlayerJoin (playerId={playerId})");
        }

        private void HandlePlayerJoinAck(ArraySegment<byte> data)
        {
            if (data.Count < PlayerJoinAckMessage.SIZE) return;

            var msg = PlayerJoinAckMessage.ReadFrom(data.Array, data.Offset);
            _isJoined = true;
            LastServerTick = msg.ServerTick;

            Debug.Log($"[KcpMovement] Join confirmed! PlayerId={msg.PlayerId}, ServerTick={msg.ServerTick}");
            OnJoinConfirmed?.Invoke(msg.PlayerId, msg.ServerTick);
        }

        private void HandleWorldSnapshot(ArraySegment<byte> data)
        {
            if (data.Count < WorldSnapshotMessage.HEADER_SIZE) return;

            var snapshot = WorldSnapshotMessage.ReadFrom(data.Array, data.Offset);
            LastServerTick = snapshot.ServerTick;

            double receiveTime = Time.timeAsDouble;

            // 处理每个玩家的快照数据
            foreach (var playerSnapshot in snapshot.Players)
            {
                if (playerSnapshot.PlayerId == playerId)
                {
                    // 本地玩家：缓存用于客户端预测校正
                    _latestLocalSnapshot = playerSnapshot;
                }
                else
                {
                    // 远程玩家：添加到快照插值缓冲
                    if (!_remoteInterpolators.TryGetValue(playerSnapshot.PlayerId, out var interpolator))
                    {
                        interpolator = new SnapshotInterpolation(interpolationDelay);
                        _remoteInterpolators[playerSnapshot.PlayerId] = interpolator;
                    }

                    interpolator.AddSnapshot(new Snapshot
                    {
                        Timestamp = receiveTime,
                        ServerTick = snapshot.ServerTick,
                        Position = playerSnapshot.Position.ToUnityVector3(),
                        Rotation = playerSnapshot.Rotation.ToUnityQuaternion(),
                        Velocity = playerSnapshot.Velocity.ToUnityVector3(),
                        IsGrounded = playerSnapshot.IsGrounded
                    });
                }
            }

            OnWorldSnapshot?.Invoke(snapshot);
        }

        private void HandlePlayerLeave(ArraySegment<byte> data)
        {
            if (data.Count < PlayerLeaveMessage.SIZE) return;

            var msg = PlayerLeaveMessage.ReadFrom(data.Array, data.Offset);
            _remoteInterpolators.Remove(msg.PlayerId);
            Debug.Log($"[KcpMovement] Player {msg.PlayerId} left");
            OnPlayerLeft?.Invoke(msg.PlayerId);
        }

        #endregion

        #region 调试信息

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUILayout.BeginArea(new Rect(10, 10, 350, 300));
            GUILayout.Label("=== KCP Movement Debug ===");
            GUILayout.Label($"Connected: {_isConnected}");
            GUILayout.Label($"Joined: {_isJoined}");
            GUILayout.Label($"Server: {serverAddress}:{serverPort}");
            GUILayout.Label($"PlayerId: {playerId}");
            GUILayout.Label($"Input Sequence: {_inputSequence}");
            GUILayout.Label($"Server Tick: {LastServerTick}");
            GUILayout.Label($"Remote Players: {_remoteInterpolators.Count}");

            if (_latestLocalSnapshot.HasValue)
            {
                var s = _latestLocalSnapshot.Value;
                GUILayout.Label($"Server Pos: {s.Position}");
            }

            foreach (var kvp in _remoteInterpolators)
            {
                GUILayout.Label($"  Player {kvp.Key}: buf={kvp.Value.BufferCount} pos={kvp.Value.CurrentPosition}");
            }

            GUILayout.EndArea();
        }

        #endregion
    }
}
