# Cube游戏服务器 - HTTP认证服务

## 概述

这是一个基于ASP.NET Core的HTTP服务器，提供了完整的用户认证功能，包括用户注册、登录和JWT令牌验证。

## 功能特性

### 🛡️ 安全特性
- **JWT令牌认证**：使用行业标准的JSON Web Tokens
- **密码加密**：SHA256哈希加密存储
- **输入验证**：严格的用户名、邮箱和密码验证
- **防重复注册**：用户名和邮箱唯一性检查
- **登录日志**：记录所有登录尝试（成功/失败）

### 🎮 游戏特色
- **初始奖励**：新用户注册获得1000游戏币和10钻石
- **用户等级系统**：支持等级和经验积分
- **多字段用户信息**：用户名、昵称、邮箱、头像等
- **账户状态管理**：支持正常、封禁、注销状态

## API接口

### 🔓 公开接口

#### 用户注册
```
POST /api/auth/register
Content-Type: application/json

{
  "username": "用户名",
  "email": "邮箱地址", 
  "password": "密码",
  "nickname": "昵称（可选）"
}
```

**响应示例：**
```json
{
  "success": true,
  "data": {
    "userId": 1,
    "username": "testuser1",
    "email": "test@example.com",
    "nickname": "测试用户",
    "createdAt": "2026-02-06T08:59:07.6217721Z"
  }
}
```

#### 用户登录
```
POST /api/auth/login
Content-Type: application/json

{
  "username": "用户名",
  "password": "密码"
}
```

**响应示例：**
```json
{
  "userId": 1,
  "username": "testuser1",
  "nickname": "测试用户",
  "email": "test@example.com",
  "level": 1,
  "experience": 0,
  "coins": 1000,
  "diamonds": 10,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-02-07T08:59:27.0000000Z"
}
```

### 🔐 受保护接口

需要在请求头中包含JWT令牌：
```
Authorization: Bearer <token>
```

#### 获取用户信息
```
GET /api/test/userinfo
Authorization: Bearer <token>
```

**响应示例：**
```json
{
  "userId": "1",
  "username": "testuser1",
  "isAuthenticated": true,
  "claims": [
    {
      "type": "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
      "value": "1"
    },
    {
      "type": "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
      "value": "testuser1"
    }
  ]
}
```

## 技术架构

### 🏗️ 核心组件

1. **AuthController** - 认证控制器
   - 处理用户注册和登录请求
   - 生成和验证JWT令牌
   - 输入验证和错误处理

2. **UserRepository** - 用户数据访问层
   - 数据库操作封装
   - 用户信息CRUD操作
   - 密码加密和验证

3. **JwtUtils** - JWT工具类
   - 令牌生成和验证
   - Claims管理
   - 安全密钥管理

4. **Database** - 数据库连接管理
   - 连接池管理
   - 异步数据库操作
   - 事务支持

### 🗄️ 数据库结构

使用MySQL/MariaDB数据库，主要表结构：

**game_users表：**
- `id` - 用户唯一标识
- `username` - 用户名（唯一）
- `email` - 邮箱地址（唯一）
- `password_hash` - 密码哈希值
- `nickname` - 昵称
- `level` - 用户等级
- `experience` - 经验值
- `coins` - 游戏币
- `diamonds` - 钻石
- `status` - 账户状态
- `last_login_time` - 最后登录时间

**login_records表：**
- 记录所有登录尝试
- 包含IP地址、用户代理等信息
- 用于安全审计

## 配置说明

### 环境变量配置 (.env文件)
```env
# 数据库配置
DATABASE_HOST=localhost
DATABASE_PORT=3306
DATABASE_NAME=cube_game
DATABASE_USER=cube_user
DATABASE_PASSWORD=cube_password

# JWT配置
JWT_SECRET_KEY=your-super-secret-jwt-key-here
JWT_ISSUER=CubeGameServer
JWT_AUDIENCE=CubeGameClient
JWT_EXPIRATION_MINUTES=1440
```

### 数据库初始化
运行 `server/sql/init_database.sql` 脚本来创建数据库表和初始数据。

## 测试验证

### 自动化测试脚本
运行 `server/final_test_auth.bat` 来执行完整的功能测试：

1. 公开接口测试
2. 受保护接口认证测试
3. 用户注册流程测试
4. 用户登录流程测试
5. JWT令牌验证测试
6. 错误处理测试

### 手动测试命令

**注册用户：**
```bash
curl -X POST http://localhost:6953/api/auth/register \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"testuser\",\"email\":\"test@example.com\",\"password\":\"password123\",\"nickname\":\"测试用户\"}"
```

**用户登录：**
```bash
curl -X POST http://localhost:6953/api/auth/login \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"testuser\",\"password\":\"password123\"}"
```

**访问受保护接口：**
```bash
curl -X GET http://localhost:6953/api/test/userinfo \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## 错误处理

系统提供详细的错误信息：

- **400 Bad Request** - 输入验证失败
- **401 Unauthorized** - 认证失败或令牌无效
- **409 Conflict** - 用户名或邮箱已存在
- **500 Internal Server Error** - 服务器内部错误

所有错误都会返回具体的错误信息帮助调试。

## 安全注意事项

⚠️ **生产环境部署前请务必：**

1. 更换默认的JWT密钥
2. 启用HTTPS
3. 配置适当的CORS策略
4. 实施速率限制防止暴力破解
5. 定期轮换数据库密码
6. 监控异常登录行为

## 开发指南

### 启动服务器
```bash
cd server/httpserver
dotnet run
```

### 项目结构
```
server/
├── httpserver/          # HTTP服务器主项目
├── Shared/             # 共享库
│   ├── Models/         # 数据模型
│   ├── Repositories/   # 数据访问层
│   └── Utils/          # 工具类
└── sql/               # 数据库脚本
```

### 扩展建议

1. **添加用户角色系统** - 支持管理员、VIP等不同权限
2. **实现密码重置功能** - 通过邮箱验证码重置密码
3. **添加第三方登录** - 微信、QQ等社交账号登录
4. **实现双因素认证** - 提高账户安全性
5. **添加用户资料编辑功能** - 头像上传、个人信息修改等

---

🎮 Happy Gaming! 🎮