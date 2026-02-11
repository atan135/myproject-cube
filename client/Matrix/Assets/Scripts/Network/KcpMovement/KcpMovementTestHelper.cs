using System.Collections.Generic;
using UnityEngine;

namespace Cube.Network.KcpMovement
{
    /// <summary>
    /// KCP 位移同步测试辅助组件
    ///
    /// 使用方法：
    /// 1. 创建一个空 GameObject，挂载此脚本
    /// 2. 挂载 KcpMovementClient 组件到同一 GameObject
    /// 3. 拖入 localPlayerObject（本地玩家模型/Cube）
    /// 4. 拖入 remotePlayerPrefab（远程玩家预制体）
    /// 5. 运行场景，在 Inspector 中点击 "Connect" 按钮或按 C 键连接
    /// 6. 使用 WASD 移动，空格跳跃，鼠标左右看
    ///
    /// 测试建议：
    /// - 同时运行两个 Unity 编辑器实例（或一个编辑器 + 一个 Build），使用不同 PlayerId
    /// - 观察远程玩家是否平滑移动（快照插值效果）
    /// - 观察 OnGUI 调试信息确认连接和数据收发
    /// </summary>
    public class KcpMovementTestHelper : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private KcpMovementClient movementClient;
        [SerializeField] private GameObject localPlayerObject;
        [SerializeField] private GameObject remotePlayerPrefab;

        [Header("移动设置")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float mouseSensitivity = 2f;

        [Header("测试设置")]
        [SerializeField] private bool autoConnect = false;
        [SerializeField] private KeyCode connectKey = KeyCode.C;
        [SerializeField] private KeyCode disconnectKey = KeyCode.X;

        // 远程玩家 GameObjects
        private readonly Dictionary<int, GameObject> _remotePlayerObjects = new();

        private float _yawAngle = 0f;
        private bool _isJumping = false;
        private CharacterController _charController;

        private void Start()
        {
            if (movementClient == null)
                movementClient = GetComponent<KcpMovementClient>();

            if (localPlayerObject != null)
                _charController = localPlayerObject.GetComponent<CharacterController>();

            // 注册事件
            if (movementClient != null)
            {
                movementClient.OnConnected += () => Debug.Log("[Test] Connected!");
                movementClient.OnDisconnected += () =>
                {
                    Debug.Log("[Test] Disconnected!");
                    ClearRemotePlayers();
                };
                movementClient.OnJoinConfirmed += (pid, tick) =>
                    Debug.Log($"[Test] Join confirmed! PlayerId={pid}, ServerTick={tick}");
                movementClient.OnWorldSnapshot += OnSnapshotReceived;
                movementClient.OnPlayerLeft += OnPlayerLeft;
            }

            if (autoConnect && movementClient != null)
            {
                movementClient.ConnectWithDefaults();
            }
        }

        private void Update()
        {
            // 快捷键
            if (Input.GetKeyDown(connectKey) && movementClient != null && !movementClient.IsConnected)
            {
                Debug.Log("[Test] Connecting...");
                movementClient.ConnectWithDefaults();
            }

            if (Input.GetKeyDown(disconnectKey) && movementClient != null && movementClient.IsConnected)
            {
                Debug.Log("[Test] Disconnecting...");
                movementClient.Disconnect();
            }

            // 本地玩家移动输入
            if (movementClient != null && movementClient.IsJoined)
            {
                HandleLocalMovement();
                UpdateRemotePlayers();
            }
        }

        /// <summary>
        /// 处理本地玩家的输入和移动
        /// </summary>
        private void HandleLocalMovement()
        {
            // 鼠标旋转
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            _yawAngle += mouseX * Mathf.Deg2Rad;

            // WASD 输入
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 direction = Vector3.zero;
            if (h != 0 || v != 0)
            {
                // 将输入转换为世界坐标方向
                Vector3 forward = new Vector3(Mathf.Sin(_yawAngle), 0, Mathf.Cos(_yawAngle));
                Vector3 right = new Vector3(Mathf.Cos(_yawAngle), 0, -Mathf.Sin(_yawAngle));
                direction = (forward * v + right * h).normalized;
            }

            // 跳跃
            _isJumping = Input.GetKeyDown(KeyCode.Space);

            // 发送位移输入到服务端
            movementClient.SendMovementInput(direction, moveSpeed, _isJumping, _yawAngle);

            // 客户端本地预测移动（不等待服务端确认）
            if (localPlayerObject != null)
            {
                if (_charController != null)
                {
                    Vector3 move = direction * moveSpeed * Time.deltaTime;
                    if (_isJumping && _charController.isGrounded)
                        move.y = 5f; // 跳跃
                    move.y += Physics.gravity.y * Time.deltaTime;
                    _charController.Move(move);
                }
                else
                {
                    // 没有 CharacterController 就直接移动 Transform
                    localPlayerObject.transform.position += direction * moveSpeed * Time.deltaTime;
                }

                // 旋转
                localPlayerObject.transform.rotation = Quaternion.Euler(0, _yawAngle * Mathf.Rad2Deg, 0);
            }

            // 服务端校正（可选：把本地玩家拉回服务端位置）
            var serverSnapshot = movementClient.GetLatestLocalSnapshot();
            if (serverSnapshot.HasValue && localPlayerObject != null)
            {
                // 简单校正：如果偏差过大就直接拉回
                Vector3 serverPos = serverSnapshot.Value.Position.ToUnityVector3();
                Vector3 localPos = localPlayerObject.transform.position;
                float distance = Vector3.Distance(serverPos, localPos);

                if (distance > 2.0f) // 偏差超过 2 米就强制校正
                {
                    localPlayerObject.transform.position = serverPos;
                    Debug.LogWarning($"[Test] Server correction! Distance={distance:F2}m");
                }
            }
        }

        /// <summary>
        /// 更新远程玩家的视觉位置（使用快照插值结果）
        /// </summary>
        private void UpdateRemotePlayers()
        {
            foreach (int remoteId in movementClient.GetRemotePlayerIds())
            {
                if (movementClient.TryGetRemotePlayerState(remoteId, out Vector3 pos, out Quaternion rot))
                {
                    // 创建或获取远程玩家对象
                    if (!_remotePlayerObjects.TryGetValue(remoteId, out var obj))
                    {
                        obj = CreateRemotePlayer(remoteId);
                        _remotePlayerObjects[remoteId] = obj;
                    }

                    // 更新位置和旋转
                    obj.transform.position = pos;
                    obj.transform.rotation = rot;
                }
            }
        }

        /// <summary>
        /// 创建远程玩家可视对象
        /// </summary>
        private GameObject CreateRemotePlayer(int remotePlayerId)
        {
            GameObject obj;
            if (remotePlayerPrefab != null)
            {
                obj = Instantiate(remotePlayerPrefab);
            }
            else
            {
                // 没有预制体就创建一个蓝色 Cube
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.blue;
                }
                // 移除碰撞体避免干扰
                var collider = obj.GetComponent<Collider>();
                if (collider != null)
                    Destroy(collider);
            }

            obj.name = $"RemotePlayer_{remotePlayerId}";
            Debug.Log($"[Test] Created remote player object for PlayerId={remotePlayerId}");
            return obj;
        }

        private void OnSnapshotReceived(WorldSnapshotMessage snapshot)
        {
            // 快照已在 KcpMovementClient 中处理
            // 这里可以添加额外的调试逻辑
        }

        private void OnPlayerLeft(int leftPlayerId)
        {
            if (_remotePlayerObjects.TryGetValue(leftPlayerId, out var obj))
            {
                Destroy(obj);
                _remotePlayerObjects.Remove(leftPlayerId);
                Debug.Log($"[Test] Remote player {leftPlayerId} object destroyed");
            }
        }

        private void ClearRemotePlayers()
        {
            foreach (var kvp in _remotePlayerObjects)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value);
            }
            _remotePlayerObjects.Clear();
        }

        private void OnDestroy()
        {
            ClearRemotePlayers();
        }
    }
}
