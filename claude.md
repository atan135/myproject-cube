# Cube - 异次元杀阵 项目概览

> 本文件供 AI 助手在新会话启动时读取，以快速了解项目全貌。

## 一、项目简介

**Cube（异次元杀阵）** 是一款融合 **生存恐怖 + MOBA 竞技** 元素的多人在线游戏。玩家在程序化生成的立方体迷宫中探索、解谜、战斗、逃生。核心玩法包括动态房间系统、陷阱机关、谜题挑战、角色技能和多种竞技模式（PvE / PvP / 大逃杀 / 猎杀）。

---

## 二、技术架构总览

```
┌─────────────────────────────────────────────────────────────┐
│                     Client (Unity)                          │
│  Unity 2022 LTS · URP · YooAsset · Wwise · UIElements      │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐                  │
│  │ HTTP     │  │ KCP/UDP  │  │ MagicOnion│                  │
│  │ (REST)   │  │ (移动同步)│  │ (gRPC/TCP)│                  │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘                  │
└───────┼─────────────┼─────────────┼─────────────────────────┘
        │             │             │
        ▼             ▼             ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│  HTTP Server │ │  GameServer  │ │  GameServer  │
│  (ASP.NET    │ │  KCP/UDP     │ │  MagicOnion  │
│   Core)      │ │  位移同步     │ │  技能/战斗    │
│  Port: 5000  │ │  Port: 7777  │ │  Port: 5001  │
└──────┬───────┘ └──────┬───────┘ └──────┬───────┘
       │                │                │
       ▼                ▼                ▼
┌─────────────────────────────────────────────────────────────┐
│  Database Layer:  MariaDB/MySQL · Redis (规划)               │
│  cube_game 数据库 · game_users/login_records/user_roles 表   │
└─────────────────────────────────────────────────────────────┘
```

**三协议并行：**
- **HTTP/HTTPS** — 登录注册、商店交易、公告查询等非实时操作
- **KCP over UDP（端口 7777）** — 高频位移同步，快照插值，允许丢包
- **MagicOnion/gRPC over TCP（端口 5001）** — 技能释放、战斗计算等敏感操作，可靠传输

**架构原则：** Server-Authoritative（所有游戏逻辑由服务端权威计算）

---

## 三、项目目录结构

```
MyProject/
├── client/                          # Unity 客户端
│   └── Matrix/
│       └── Assets/
│           ├── Scripts/
│           │   ├── Data/            # 数据实体（Entity_User 等）
│           │   ├── Framework/       # 框架层
│           │   │   ├── Db/          # 配置表服务 (SQLite + CSV Entity)
│           │   │   └── YooAssetLoader/  # YooAsset 资源初始化
│           │   ├── Game/            # 游戏逻辑层
│           │   │   ├── Character/   # 角色控制器
│           │   │   ├── Combat/      # 战斗系统
│           │   │   ├── Loading/     # 加载/场景切换模块
│           │   │   ├── Login/       # 登录控制器 & 登录业务模块
│           │   │   ├── Maze/        # 迷宫生成器
│           │   │   ├── UI/          # UIManager (基于 UIElements)
│           │   │   └── GameStart.cs # 客户端入口
│           │   ├── Network/         # 网络层
│           │   │   ├── HttpManager.cs       # HTTP 请求管理
│           │   │   ├── NetworkManager.cs    # TCP 网络管理 (骨架)
│           │   │   └── KcpMovement/         # KCP 位移同步客户端
│           │   │       ├── KcpMovementClient.cs  # 客户端主逻辑
│           │   │       ├── MovementMessages.cs   # 消息协议定义
│           │   │       └── SnapshotInterpolation.cs  # 快照插值
│           │   ├── Tools/           # 工具脚本（相机、输入测试等）
│           │   └── Utility/         # 通用工具（Logger, MathHelper, Extensions）
│           └── ...                  # Unity 资源、场景、UI 等
│
├── server/                          # .NET 服务端
│   ├── Cube.sln                     # 解决方案文件
│   ├── appsettings.json             # 全局配置（DB、JWT、GameServer）
│   ├── httpserver/                  # HTTP API 服务
│   │   ├── Program.cs              # 入口：ASP.NET Core + JWT + CORS
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs   # 登录/注册 API
│   │   │   ├── TradeController.cs  # 商店/购买 API (骨架)
│   │   │   └── TestController.cs   # 测试接口
│   │   └── appsettings.*.json
│   ├── gameserver/                  # 实时游戏服务
│   │   ├── Program.cs              # 入口：MagicOnion + KCP 双协议启动
│   │   ├── GameRoom.cs             # 游戏房间管理
│   │   ├── KcpTransport/
│   │   │   ├── KcpMovementServer.cs  # KCP 位移同步服务端
│   │   │   └── MovementMessage.cs    # 位移消息协议
│   │   ├── lib/kcp/                # kcp2k 库源码（KCP 网络传输）
│   │   └── appsettings.json        # GameServer 配置（端口、TickRate）
│   ├── Shared/                      # 共享库 (Cube.Shared)
│   │   ├── Models/                  # 数据模型
│   │   │   ├── User.cs             # 用户模型 (game_users 表映射)
│   │   │   ├── Character.cs        # 角色模型
│   │   │   └── GameRecord.cs       # 游戏记录模型
│   │   ├── Protocols/
│   │   │   └── Message.cs          # 通用消息基类 & MessageType 枚举
│   │   ├── Repositories/
│   │   │   └── UserRepository.cs   # 用户数据访问层 (CRUD + 密码验证)
│   │   ├── Utils/
│   │   │   ├── Database.cs         # MariaDB 数据库工具 (连接池 + 异步操作)
│   │   │   ├── JwtUtils.cs         # JWT 令牌生成/验证
│   │   │   ├── SimpleConfig.cs     # 配置加载 (appsettings + .env)
│   │   │   ├── Logger.cs           # 日志工具
│   │   │   └── LogUtils.cs         # 日志辅助
│   │   ├── Data/                   # 共享数据实体 (CSV 工具生成)
│   │   ├── Examples/               # 工具使用示例
│   │   └── Matrix.Shared.cs        # BaseResponse<T> 统一响应格式
│   └── sql/
│       └── init_database.sql       # 数据库初始化脚本 (MariaDB/MySQL)
│
├── tools/                           # 工具链
│   ├── CsvCreate/                   # CSV 多语言测试数据生成器 (Python)
│   │   ├── csvcreate.py             # 使用 Faker 生成多语言 CSV
│   │   └── Generated_L10N/          # 生成的本地化 CSV 文件
│   │       ├── zh_CN/ en_US/ ja_JP/ ru_RU/ ar_SA/
│   └── CsvTools/                    # CSV → SQLite 转换工具 (.NET)
│       ├── Program.cs               # 入口
│       ├── CsvParser/CsvParser.cs   # 解析 CSV → 生成 Entity C# 代码 + SQLite .db
│       └── ConfigReader.cs          # 配置读取
│
├── doc/                             # 项目文档
│   ├── project-startup-plan.md      # 项目启动计划（完整）
│   ├── kcp-movement-test-guide.md   # KCP 位移测试指南
│   ├── 基础规范/
│   │   └── git提交规范.md            # Git Commit 规范 (Conventional Commits)
│   ├── 客户端技术文档/
│   │   ├── 客户端技术文档.md          # 客户端技术方案（详细）
│   │   └── 大纲.md
│   ├── 服务端技术文档/
│   │   ├── 服务端技术文档.md          # 服务端技术方案（详细）
│   │   ├── 服务端热更新机制.md
│   │   └── 大纲.md
│   ├── 项目设计/
│   │   ├── README.md                # 设计文档总目录
│   │   ├── 初始设计/                # 初版设计文档 (6 篇)
│   │   └── 202602重新设计/          # 重新设计文档 (玩法/留存/商业化)
│   └── 资源网站/                    # 外部素材资源链接
│
└── claude.md                        # ← 本文件
```

---

## 四、客户端详解（Unity）

### 4.1 技术栈
| 技术 | 用途 |
|------|------|
| **Unity 2022 LTS** | 游戏引擎 |
| **URP** | 通用渲染管线 |
| **YooAsset** | 资源管理（热更新、AssetBundle） |
| **Wwise** | 专业音频引擎（规划中） |
| **UIElements** | UI 系统（替代 UGUI） |
| **kcp2k** | KCP 网络传输 |
| **SQLite** | 本地配置表数据库 |

### 4.2 启动流程
1. `GameStart.cs` → 调用 `YooAssetLauncher.InitializeYooAsset()` 初始化资源系统
2. YooAsset 支持三种模式：`EditorSimulateMode`（编辑器）、`OfflinePlayMode`（离线）、`HostPlayMode`（热更新）
3. 通过 `UIManager.OpenScreen("LoginWindow")` 加载登录界面（UXML）
4. 创建 `LoginController` 绑定 UI 事件

### 4.3 登录流程
1. `LoginController` 获取 UI 输入 → 调用 `LoginModule.LoginAsync()`
2. `LoginModule` 通过 `HttpManager.PostAsync<LoginResponse>("/api/auth/login", ...)` 发送 HTTP 请求
3. 登录成功后：保存 JWT Token → 缓存用户信息 → 通过 `LoadingModule` 切换到主城场景

### 4.4 网络模块
- **HttpManager**：单例，处理所有 REST API 请求，自动附加 JWT Authorization Header，使用 `BaseResponse<T>` 统一反序列化
- **KcpMovementClient**：KCP/UDP 客户端，负责：
  - 连接服务端 → 发送 PlayerJoin（Reliable 通道）
  - 高频发送位移输入（Unreliable 通道，30Hz）
  - 接收世界快照 → 本地玩家服务端校正 + 远程玩家快照插值
  - `SnapshotInterpolation` 实现平滑的远程玩家运动表现
- **NetworkManager**：TCP 网络管理器（骨架，待实现）

### 4.5 配置表系统
- **工具链**：`CsvCreate`（Python 生成多语言 CSV）→ `CsvTools`（.NET 解析 CSV → 生成 Entity C# 类 + SQLite .db 文件）
- **运行时**：`ConfigService`（单例）加载 SQLite 数据库，支持：
  - 按语言切换数据库（`zh_CN.db` / `en_US.db` 等）
  - 常用小表预加载到内存 `Dictionary<int, T>`（O(1) 查询）
  - 大表按需查询磁盘
  - Android 平台自动拷贝 DB 到持久化目录

### 4.6 游戏逻辑模块（部分为骨架）
- **CharacterController**：角色基类，管理移动速度、生命值、受伤/死亡逻辑
- **CombatSystem**：战斗系统，伤害计算（`baseDamage - defense`）
- **MazeGenerator**：迷宫程序化生成器（骨架，算法待实现）
- **LoadingModule**：场景加载 + 进度条 UI（通过 YooAsset 异步加载场景）

---

## 五、服务端详解（.NET 8）

### 5.1 解决方案结构
```
Cube.sln
├── Cube.HttpServer    # ASP.NET Core Web API
├── Cube.GameServer    # 实时游戏服务器
└── Cube.Shared        # 共享库（模型、工具、仓储）
```

### 5.2 HTTP 服务 (httpserver)
- **框架**：ASP.NET Core
- **端口**：默认 5000（HTTPS 5001 可配置）
- **认证**：JWT Bearer Token
- **API 端点**：
  - `POST /api/auth/login` — 用户登录（用户名+密码 → 返回 JWT + UserInfo）
  - `POST /api/auth/register` — 用户注册（用户名+邮箱+密码）
  - `POST /api/trade/purchase` — 购买物品（骨架）
  - `GET /api/trade/shop` — 获取商店列表（骨架）
  - `GET /api/test/*` — 测试接口
- **安全**：密码 SHA256 哈希、输入验证、登录记录审计、CORS 配置

### 5.3 游戏服务 (gameserver)
- **双协议架构**（`Program.cs` 同时启动）：
  - **MagicOnion (gRPC/TCP, 端口 5001)** — 技能释放、战斗计算等可靠操作
  - **KcpMovementServer (KCP/UDP, 端口 7777)** — 位移同步
- **KcpMovementServer 核心逻辑**：
  - 管理所有玩家状态（位置、旋转、速度、是否着地）
  - 接收客户端输入 → 服务端权威物理计算（重力、地面检测）
  - 固定 20Hz 向所有客户端广播世界快照
  - 使用 `ConcurrentDictionary` 线程安全管理玩家状态
- **GameRoom**：
  - 管理单个游戏房间的客户端列表和生命周期
  - 以 20 tick/s 的频率运行游戏逻辑循环
  - 位移由 KcpMovementServer 独立处理，GameRoom 专注技能/战斗逻辑
  - 可通过 `MovementServer.GetPlayerState()` 获取玩家位置进行战斗判定

### 5.4 共享库 (Shared)
- **Models**：`User`（映射 `game_users` 表）、`Character`（角色）、`GameRecord`（游戏记录）
- **Repositories**：`UserRepository`（用户 CRUD、密码验证、登录记录）
- **Protocols**：`Message` 基类 + `MessageType` 枚举（Connect/Match/Game 消息类型）
- **Utils**：
  - `Database` — MariaDB 连接池 + 异步 SQL 操作（NonQuery/Scalar/Reader/Transaction）
  - `JwtUtils` — JWT 生成/验证（HS256，配置化密钥和过期时间）
  - `SimpleConfig` — 多层配置加载（appsettings.json + 环境变量 + .env）
  - `Logger` / `LogUtils` — 日志工具
- **BaseResponse\<T\>** — 统一 API 响应格式 `{ code, msg, data }`

### 5.5 数据库
- **数据库**：MariaDB/MySQL（`cube_game`），字符集 `utf8mb4`
- **核心表**：
  - `game_users` — 用户信息（用户名、邮箱、密码哈希、昵称、等级、经验、金币、钻石、状态）
  - `login_records` — 登录记录（IP、UserAgent、时间、结果、失败原因）
  - `user_roles` — 角色权限（player/vip/moderator/admin）
  - `user_role_relations` — 用户-角色关联
- **配置**：`appsettings.json` 中 `Database` 节 → host/port/name/user/password

---

## 六、配置表工具链

### 6.1 数据流
```
策划 CSV 编辑 → CsvCreate(Python) 生成多语言 CSV → CsvTools(.NET) 转换 → SQLite .db + Entity C#
                                                                                  ↓
                                                              客户端 ConfigService 加载
```

### 6.2 CsvCreate（Python）
- 使用 `Faker` 库为 5 种语言（zh_CN/en_US/ja_JP/ru_RU/ar_SA）生成测试数据
- 支持数据类型：int, float, string, int64, Dict, Array
- Dict/Array 类型全球统一使用英文，string 类型按语言本地化

### 6.3 CsvTools（.NET）
- 读取基准语言（en_US）的 CSV 结构 → 自动生成 `Entity_csv.cs`（C# 实体类，SQLite ORM 映射）
- 遍历所有语言 CSV → 生成对应的 `{locale}.db` SQLite 数据库文件
- 含数据校验（类型检查）和 VACUUM 优化

---

## 七、开发规范

### 7.1 Git 提交规范
遵循 **Conventional Commits** 格式：
```
<type>(<scope>): <subject>
```
- Type：feat / fix / docs / style / refactor / perf / test / chore / build / ci / revert
- Scope（客户端）：ui / combat / maze / character / network / framework / render / audio / asset
- Scope（服务端）：gateway / auth / match / game / data / database / api
- 提交语言：中文

### 7.2 代码规范要点
- **客户端**：C# Unity 风格，单例模式广泛使用（`UIManager`, `HttpManager`, `LoginModule`, `ConfigService`）
- **服务端**：.NET 8 异步编程，Repository 模式，依赖注入
- **命名空间**：客户端 `Cube.Game.*` / `Cube.Network.*`，服务端 `Cube.Shared.*` / `Cube.HttpServer.*` / `Cube.GameServer.*`
- **响应格式**：统一使用 `BaseResponse<T>` (`Matrix.Shared` 命名空间)

---

## 八、关键配置

### 8.1 服务端全局配置 (`server/appsettings.json`)
```json
{
  "Database": { "Host": "localhost", "Port": 3306, "Name": "cube_game" },
  "Jwt": { "SecretKey": "...", "Issuer": "CubeGameServer", "ExpirationMinutes": 60 },
  "GameServer": { "Port": 8888, "TickRate": 20, "MaxClientsPerRoom": 8 }
}
```

### 8.2 GameServer 配置 (`server/gameserver/appsettings.json`)
```json
{
  "GameServer": {
    "MagicOnionPort": 5001,
    "KcpPort": 7777,
    "SnapshotRate": 20,
    "HeartbeatInterval": 30
  }
}
```

---

## 九、当前实现状态

### ✅ 已实现
- 客户端：YooAsset 资源初始化（三种模式）、UIManager（UIElements 加载/卸载）、登录流程（UI → HTTP → Token → 场景切换）、KCP 位移同步客户端（连接/输入发送/快照插值）、配置表系统（SQLite 加载/查询/预加载）、Loading 模块
- 服务端：HTTP Server（JWT 认证 + 登录注册 API）、GameServer 双协议启动、KCP 位移同步服务端（物理计算 + 快照广播）、GameRoom 基础框架、数据库工具（连接池/异步操作）、用户仓储（完整 CRUD）
- 工具链：CSV 多语言数据生成 + SQLite 转换

### 🚧 骨架/待实现
- 客户端：NetworkManager（TCP）、MazeGenerator（迷宫算法）、CombatSystem（具体战斗逻辑）、CharacterController（移动实现）
- 服务端：TradeController（商店/购买）、匹配系统、MagicOnion 服务实现（技能/战斗 RPC）、Redis 集成、MongoDB 日志
- 游戏系统：陷阱系统、谜题系统、技能树、竞技模式、社交系统

---

## 十、开发路线图（摘要）

| 阶段 | 内容 |
|------|------|
| **Phase 1** | 核心框架搭建（网络、资源、UI）、基础登录注册、迷宫原型 |
| **Phase 2** | 核心玩法实现（迷宫生成、角色移动、基础战斗、陷阱/谜题） |
| **Phase 3** | 竞技系统（匹配、PvP、技能树）、社交功能 |
| **Phase 4** | 商业化（商店/内购）、性能优化、正式发布准备 |

---

## 十一、快速上手

### 运行服务端
```bash
# 1. 初始化数据库
mysql -u root -p < server/sql/init_database.sql

# 2. 启动 HTTP Server
cd server/httpserver
dotnet run

# 3. 启动 Game Server（另一个终端）
cd server/gameserver
dotnet run
```

### 运行客户端
1. 使用 Unity 2022 LTS 打开 `client/Matrix/` 项目
2. 确保 YooAsset 包已安装
3. 设置 `YooAssetLauncher` 的 `loadMode` 为 `EditorSimulateMode`
4. 运行 `GameStart` 所在场景

### 运行配置表工具
```bash
# 生成测试 CSV（需要 Python + Faker）
cd tools/CsvCreate
python csvcreate.py

# CSV 转 SQLite + Entity
cd tools/CsvTools
dotnet run
```
