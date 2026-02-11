# KCP 位移同步测试指南

## 1. 架构概览

```
┌─────────────┐     TCP/gRPC (MagicOnion)     ┌──────────────┐
│  Unity 客户端 │ ◄──────────────────────────► │  GameServer   │
│             │     UDP/KCP (位移同步)          │              │
│             │ ◄──────────────────────────► │              │
└─────────────┘                                └──────────────┘

MagicOnion (TCP): 技能、战斗等敏感操作 → 端口 5001
KCP (UDP):        位移同步、快照插值     → 端口 7777
```

### 通信流程

```
客户端                                    服务端
  │                                         │
  │──── KCP Handshake (Hello) ────────────►│  UDP 连接建立
  │◄─── KCP Handshake (Hello+Cookie) ──────│
  │                                         │
  │──── PlayerJoin (Reliable) ────────────►│  绑定 PlayerId
  │◄─── PlayerJoinAck (Reliable) ──────────│
  │                                         │
  │──── MovementInput (Unreliable, 30Hz)──►│  高频发送输入
  │◄─── WorldSnapshot (Unreliable, 20Hz)───│  服务端广播快照
  │◄─── WorldSnapshot ─────────────────────│
  │──── MovementInput ─────────────────────►│
  │     ...                                 │
```

## 2. 服务端配置与启动

### 2.1 配置文件

编辑 `server/gameserver/appsettings.json`：

```json
{
  "GameServer": {
    "MagicOnionPort": 5001,
    "KcpPort": 7777,
    "SnapshotRate": 20,
    "MaxClientsPerRoom": 8
  }
}
```

| 配置项 | 说明 | 默认值 |
|--------|------|--------|
| MagicOnionPort | MagicOnion gRPC 端口 (TCP) | 5001 |
| KcpPort | KCP 位移同步端口 (UDP) | 7777 |
| SnapshotRate | 世界快照广播频率 (Hz) | 20 |
| MaxClientsPerRoom | 每房间最大客户端数 | 8 |

### 2.2 编译与启动服务端

```bash
cd server/gameserver
dotnet build
dotnet run
```

启动成功后你会看到：

```
=== Game Server Starting (Dual Protocol) ===
=== Game Server Configuration ===
  MagicOnion Port (TCP/gRPC): 5001
  KCP Port (UDP): 7777
  Snapshot Rate: 20 Hz
  Max Clients Per Room: 8
=================================
[KcpMovement] Server started on UDP port 7777, snapshot rate: 20Hz
[KcpMovement] Tick loop started
[MagicOnion] gRPC server starting on port 5001 (HTTP/2)
=== All services started ===
  MagicOnion (TCP): port 5001
  KCP Movement (UDP): port 7777
  Snapshot Rate: 20 Hz
Press Ctrl+C to stop the server...
```

### 2.3 验证服务端 UDP 端口

Windows:
```powershell
netstat -an | findstr 7777
```

应该看到：
```
UDP    0.0.0.0:7777    *:*
```

## 3. Unity 客户端配置

### 3.1 创建测试场景

1. **创建新场景**（或使用 `Assets/test/test.unity`）

2. **创建地面**
   - 创建 Plane：`GameObject → 3D Object → Plane`
   - 位置设为 `(0, 0, 0)`
   - 缩放设为 `(10, 1, 10)`

3. **创建本地玩家**
   - 创建 Cube：`GameObject → 3D Object → Cube`
   - 命名为 `LocalPlayer`
   - 位置设为 `(0, 0.5, 0)`
   - 给它一个绿色材质（便于区分）
   - 可选：添加 `CharacterController` 组件

4. **创建网络管理器**
   - 创建空 GameObject，命名为 `KcpMovementManager`
   - 添加组件：`KcpMovementClient`
   - 添加组件：`KcpMovementTestHelper`

5. **配置 KcpMovementClient 组件**

   | 属性 | 值 | 说明 |
   |------|-----|------|
   | Server Address | `127.0.0.1` | 本地测试 |
   | Server Port | `7777` | KCP UDP 端口 |
   | Player Id | `1` | 第一个客户端设为 1 |
   | Input Send Rate | `30` | 每秒发送 30 次输入 |
   | Interpolation Delay | `0.1` | 100ms 插值延迟 |
   | Show Debug Info | `✓` | 显示调试信息 |

6. **配置 KcpMovementTestHelper 组件**

   | 属性 | 值 | 说明 |
   |------|-----|------|
   | Movement Client | 拖入同对象的 KcpMovementClient |  |
   | Local Player Object | 拖入 `LocalPlayer` Cube |  |
   | Remote Player Prefab | 留空（自动创建蓝色 Cube） |  |
   | Move Speed | `5` |  |
   | Auto Connect | `✓`（或用按键手动连接） |  |
   | Connect Key | `C` | 按 C 连接 |
   | Disconnect Key | `X` | 按 X 断开 |

### 3.2 场景层级结构

```
Hierarchy:
├── Main Camera
├── Directional Light
├── Ground (Plane)
├── LocalPlayer (Cube, 绿色)
└── KcpMovementManager
    ├── KcpMovementClient (Script)
    └── KcpMovementTestHelper (Script)
```

## 4. 测试步骤

### 4.1 单客户端基础连接测试

**目的**: 验证 KCP 连接、PlayerJoin、快照收发

1. 启动服务端：`dotnet run`
2. 在 Unity 编辑器中运行场景
3. 按 `C` 键连接（或勾选 Auto Connect）
4. 观察左上角调试信息：
   - `Connected: True` → KCP 连接成功
   - `Joined: True` → PlayerJoin 确认
   - `Server Tick: N` → 快照正在接收（数字递增）
   - `Server Pos: (x, y, z)` → 服务端认定的位置

5. 用 WASD 移动、空格跳跃
6. 观察服务端控制台输出：
   ```
   [KcpMovement] KCP connection established: XXXXX
   [KcpMovement] Player 1 joining via connection XXXXX
   [KcpMovement] Player 1 joined successfully. Total players: 1
   ```

**预期结果**:
- ✅ Unity 左上角显示连接状态和服务端 Tick
- ✅ 本地 Cube 可用 WASD 移动
- ✅ 服务端打印连接和加入日志
- ✅ `Server Pos` 随移动更新

### 4.2 双客户端位移同步测试

**目的**: 验证多客户端之间的位移同步和快照插值

#### 方法 A：Build + Editor 同时运行

1. `File → Build Settings → Build` 编译一个独立客户端
2. 修改其中一个客户端的 PlayerId（Editor 中设为 1，Build 设为 2）
   > **提示**: 可以创建一个简单的 UI InputField 让用户输入 PlayerId

3. 启动服务端
4. 先运行 Build 的客户端（PlayerId=2）
5. 再在 Editor 中运行（PlayerId=1）
6. 两边都按 `C` 连接

#### 方法 B：通过代码设置不同 PlayerId

在 `KcpMovementTestHelper.Start()` 中临时添加：
```csharp
// 用命令行参数区分，例如 -playerId 2
var args = System.Environment.GetCommandLineArgs();
for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] == "-playerId")
    {
        movementClient.Connect(serverAddress, serverPort, int.Parse(args[i+1]));
        return;
    }
}
```

**测试操作**:

1. 两个客户端都连接后，在客户端 1 中移动
2. 观察客户端 2 中是否出现蓝色 Cube（远程玩家）
3. 蓝色 Cube 是否跟随客户端 1 的移动平滑移动
4. 反向操作：在客户端 2 中移动，观察客户端 1

**预期结果**:
- ✅ 对方的蓝色 Cube 出现在场景中
- ✅ 远程玩家移动是平滑的（快照插值效果）
- ✅ 调试信息显示 `Remote Players: 1`
- ✅ 一方断开后，另一方的远程 Cube 消失

### 4.3 网络延迟模拟测试

**目的**: 验证在弱网环境下快照插值的效果

Windows 上可以用 `clumsy` 工具模拟网络延迟：
- 下载 [Clumsy](https://jagt.github.io/clumsy/)
- 设置 `Lag: 100ms`，`Drop: 5%`
- 观察远程玩家是否仍然平滑移动

**预期结果**:
- ✅ 100ms 延迟下远程玩家仍然基本平滑
- ✅ 5% 丢包下远程玩家有轻微卡顿但可接受
- ✅ 不会出现断连（KCP 有重传机制）

## 5. 常见问题排查

### Q: Unity 连接后 Connected 一直是 False

**检查**:
1. 服务端是否在运行且 UDP 7777 端口已绑定
2. 防火墙是否放行 UDP 7777（入站规则）
3. IP 地址是否正确（本地测试用 `127.0.0.1`）

```powershell
# 添加防火墙规则
New-NetFirewallRule -DisplayName "GameServer KCP" -Direction Inbound -Protocol UDP -LocalPort 7777 -Action Allow
```

### Q: Connected 但 Joined 一直是 False

**检查**:
1. 查看服务端日志是否收到 PlayerJoin 消息
2. 确认 PlayerId 是否有效（> 0）
3. 检查是否有 KCP 错误日志

### Q: 远程玩家不出现

**检查**:
1. 两个客户端是否使用了不同的 PlayerId
2. 服务端日志是否显示两个玩家都已加入
3. 调试信息中 `Remote Players` 数量是否 > 0

### Q: 远程玩家移动卡顿/抖动

**调整**:
1. 增大 `Interpolation Delay`（如 0.15 或 0.2）
2. 检查服务端 SnapshotRate 是否为 20
3. 检查网络是否有丢包

### Q: 本地玩家位置频繁被服务端拉回

**说明**:
- 这是服务端权威校正的正常行为
- `KcpMovementTestHelper` 中的校正阈值默认 2 米
- 如果觉得频繁，可以增大阈值或降低校正频率

## 6. 关键文件说明

### 服务端文件

| 文件 | 说明 |
|------|------|
| `server/gameserver/Program.cs` | 服务器入口，启动 MagicOnion + KCP |
| `server/gameserver/KcpTransport/KcpMovementServer.cs` | KCP 位移同步服务主逻辑 |
| `server/gameserver/KcpTransport/MovementMessage.cs` | 网络消息协议定义 |
| `server/gameserver/GameRoom.cs` | 游戏房间，集成 KCP 位移 |
| `server/gameserver/lib/kcp/` | KCP 底层库（低层+高层） |
| `server/gameserver/appsettings.json` | 服务器配置 |

### 客户端文件

| 文件 | 说明 |
|------|------|
| `Assets/Scripts/Network/KcpMovement/KcpMovementClient.cs` | KCP 位移客户端核心 |
| `Assets/Scripts/Network/KcpMovement/MovementMessage.cs` | 网络消息协议（与服务端一致） |
| `Assets/Scripts/Network/KcpMovement/SnapshotInterpolation.cs` | 快照插值算法 |
| `Assets/Scripts/Network/KcpMovement/KcpMovementTestHelper.cs` | 测试辅助组件 |
| `Assets/lib/kcp/` | KCP 底层库 |

## 7. 性能参数参考

| 参数 | 说明 | 推荐值 |
|------|------|--------|
| KCP Interval | KCP 内部更新间隔 | 10ms |
| Input Send Rate | 客户端输入发送频率 | 30Hz |
| Snapshot Rate | 服务端快照广播频率 | 20Hz |
| Interpolation Delay | 客户端快照插值延迟 | 100ms (2帧@20Hz) |
| MTU | 最大传输单元 | 1200 bytes |
| 单个快照大小 | 每个玩家 45 bytes | 8人 ≈ 375 bytes |
| 带宽 (服务端→客户端) | 快照 × 频率 | ~7.5 KB/s per client (8人) |
| 带宽 (客户端→服务端) | 输入 × 频率 | ~0.9 KB/s per client |

## 8. 后续扩展

- [ ] **客户端预测 + 服务端回滚**: 使用 InputSequence 进行更精确的校正
- [ ] **区域兴趣管理 (AOI)**: 只发送视野范围内的玩家快照
- [ ] **快照压缩**: 使用 Delta 压缩减少带宽（只发送变化的字段）
- [ ] **MagicOnion 战斗集成**: 技能释放通过 TCP 发送，伤害结果通过 TCP 广播
- [ ] **房间管理**: 将 KCP 玩家状态与 GameRoom 关联
