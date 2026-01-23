# Cube 客户端项目

## 项目信息

- **引擎版本**: Unity 2022 LTS 或更高
- **渲染管线**: URP (Universal Render Pipeline)
- **脚本后端**: IL2CPP (发布版本)
- **目标平台**: PC (Windows/Mac) 优先

## 项目结构

```
Assets/
├── Scripts/              # 脚本代码
│   ├── Framework/       # 框架层
│   ├── Game/           # 游戏逻辑
│   │   ├── Character/  # 角色系统
│   │   ├── Maze/       # 迷宫系统
│   │   ├── Combat/     # 战斗系统
│   │   └── UI/         # UI逻辑
│   ├── Network/        # 网络模块
│   └── Utility/        # 工具类
├── Art/                 # 美术资源
│   ├── Models/         # 3D模型
│   ├── Textures/       # 贴图
│   ├── Materials/        # 材质
│   └── Animations/     # 动画
├── Audio/               # 音频资源
├── Prefabs/             # 预制体
└── Scenes/              # 场景文件
```

## 开发环境要求

- Unity 2022.3 LTS 或更高版本
- Visual Studio 2022 或 Rider
- Git

## 快速开始

1. 使用 Unity Hub 打开项目
2. 等待 Unity 导入所有资源
3. 打开 `Assets/Scenes/SampleScene.unity`
4. 点击 Play 按钮开始测试

## 核心系统

### 角色系统
- 支持多种角色类型（坦克、输出、辅助、侦察）
- 每个角色有独特的技能和属性

### 迷宫系统
- 程序化生成立方体迷宫
- 多种房间类型和陷阱

### 战斗系统
- 实时战斗
- 技能释放
- 伤害计算

### 网络系统
- 与服务器通信
- 状态同步
- 消息处理

## 开发规范

- 使用 C# 命名空间组织代码
- 遵循 Unity 编码规范
- 编写清晰的注释
- 保持代码简洁和可维护
