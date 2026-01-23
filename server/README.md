# Cube 服务端项目

## 项目结构

```
server/
├── httpserver/          # HTTP 服务器
│   ├── Controllers/     # API 控制器
│   ├── Program.cs       # 程序入口
│   └── Cube.HttpServer.csproj
├── gameserver/          # 游戏服务器
│   ├── GameRoom.cs      # 游戏房间管理
│   ├── Program.cs       # 程序入口
│   └── Cube.GameServer.csproj
├── Shared/              # 共享代码
│   ├── Models/         # 数据模型
│   ├── Protocols/      # 协议定义
│   └── Utils/          # 工具类
├── docs/                # API文档
└── Cube.sln            # 解决方案文件
```

## 技术栈

- **.NET 8** (LTS)
- **ASP.NET Core** (HTTP 服务器)
- **TCP Socket** (游戏服务器)
- **PostgreSQL** (主数据库，待集成)
- **Redis** (缓存，待集成)

## 服务说明

### HttpServer (HTTP 服务器)

负责处理游戏内的 HTTP 请求，包括：

- **认证相关** (`/api/auth`)
  - 用户登录
  - 用户注册
  - Token 验证

- **交易相关** (`/api/trade`)
  - 商店购买
  - 物品交易
  - 数据查询

- **其他 HTTP 请求**
  - 排行榜查询
  - 用户信息查询
  - 游戏记录查询

**运行方式**：
```bash
cd httpserver
dotnet run
```

默认端口：`5000` (HTTP) / `5001` (HTTPS)

### GameServer (游戏服务器)

负责处理游戏内的帧同步请求，包括：

- **连接管理**
  - 客户端连接/断开
  - 心跳检测

- **游戏逻辑**
  - 房间管理
  - 游戏状态同步
  - 帧同步处理
  - 玩家操作验证

- **消息处理**
  - 玩家移动
  - 技能释放
  - 状态更新

**运行方式**：
```bash
cd gameserver
dotnet run
```

默认端口：`8888` (TCP)

### Shared (共享代码)

包含服务端共享的代码：

- **Models/** - 数据模型（User, Character, GameRecord 等）
- **Protocols/** - 网络协议定义（Message, MessageType 等）
- **Utils/** - 工具类（Logger 等）

## 开发环境要求

- .NET 8 SDK
- Visual Studio 2022 或 VS Code
- PostgreSQL 14+ (待集成)
- Redis 6+ (待集成)

## 快速开始

### 1. 恢复依赖

```bash
dotnet restore
```

### 2. 构建项目

```bash
dotnet build
```

### 3. 运行服务

**运行 HTTP 服务器**：
```bash
cd httpserver
dotnet run
```

**运行游戏服务器**（新终端）：
```bash
cd gameserver
dotnet run
```

### 4. 测试 API

HTTP 服务器启动后，可以访问：
- Swagger UI: `https://localhost:5001/swagger` (开发环境)
- API 端点: `https://localhost:5001/api/auth/login`

## 开发规范

- 遵循 C# 编码规范
- 使用 async/await 处理异步操作
- 统一使用 Shared 项目中的模型和协议
- HTTP 请求使用 RESTful API 设计
- 游戏服务器使用 TCP Socket 进行实时通信
- 编写单元测试覆盖核心逻辑

## 架构设计

### HTTP 服务器架构

```
客户端
  ↓ HTTP/HTTPS
HttpServer (ASP.NET Core)
  ↓
Controllers (API 端点)
  ↓
Services (业务逻辑)
  ↓
Database (数据持久化)
```

### 游戏服务器架构

```
客户端
  ↓ TCP Socket
GameServer
  ↓
GameRoom (房间管理)
  ↓
GameLogic (游戏逻辑)
  ↓
StateSync (状态同步)
```

## 后续开发计划

1. **数据库集成**
   - 集成 PostgreSQL
   - 实现数据访问层
   - 数据库迁移

2. **认证系统**
   - JWT Token 生成和验证
   - 用户密码加密
   - 权限管理

3. **游戏逻辑**
   - 完整的房间管理系统
   - 帧同步实现
   - 游戏状态管理

4. **性能优化**
   - 连接池管理
   - 消息序列化优化
   - 服务器负载均衡
